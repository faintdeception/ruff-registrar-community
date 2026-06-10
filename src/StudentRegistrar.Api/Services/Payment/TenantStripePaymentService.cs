using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services.Infrastructure;
using StudentRegistrar.Data;
using StudentRegistrar.Data.Repositories;
using StudentRegistrar.Models;

namespace StudentRegistrar.Api.Services;

public interface ITenantStripePaymentService
{
    Task<TenantStripeCheckoutSessionDto> CreateCheckoutSessionAsync(
        CreateTenantStripeCheckoutSessionDto request,
        CancellationToken cancellationToken = default);

    Task<TenantStripeWebhookResultDto> HandleWebhookAsync(
        string payload,
        string signatureHeader,
        CancellationToken cancellationToken = default);
}

public sealed class TenantStripePaymentService : ITenantStripePaymentService
{
    private const string SettledMarker = "[stripe-settled]";
    private const string FailedMarker = "[stripe-failed]";
    private const string RefundedMarker = "[stripe-refund]";

    private readonly StudentRegistrarDbContext _dbContext;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ITenantStripePaymentGateway _stripePaymentGateway;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TenantStripePaymentService> _logger;

    public TenantStripePaymentService(
        StudentRegistrarDbContext dbContext,
        IPaymentRepository paymentRepository,
        ITenantStripePaymentGateway stripePaymentGateway,
        ITenantContextAccessor tenantContextAccessor,
        IConfiguration configuration,
        ILogger<TenantStripePaymentService> logger)
    {
        _dbContext = dbContext;
        _paymentRepository = paymentRepository;
        _stripePaymentGateway = stripePaymentGateway;
        _tenantContextAccessor = tenantContextAccessor;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<TenantStripeCheckoutSessionDto> CreateCheckoutSessionAsync(
        CreateTenantStripeCheckoutSessionDto request,
        CancellationToken cancellationToken = default)
    {
        var tenant = await GetCurrentTenantAsync(cancellationToken);
        EnsureConnectReadyForCollection(tenant);

        var accountHolder = await _dbContext.AccountHolders
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AccountHolderId && a.TenantId == tenant.Id, cancellationToken)
            ?? throw new InvalidOperationException("Account holder was not found for this tenant.");

        if (request.EnrollmentId.HasValue)
        {
            var enrollmentExists = await _dbContext.Enrollments
                .AsNoTracking()
                .AnyAsync(e => e.Id == request.EnrollmentId.Value && e.TenantId == tenant.Id, cancellationToken);

            if (!enrollmentExists)
            {
                throw new InvalidOperationException("Enrollment was not found for this tenant.");
            }
        }

        Payment payment;

        if (request.PaymentId.HasValue)
        {
            payment = await _paymentRepository.GetByIdAsync(request.PaymentId.Value)
                ?? throw new InvalidOperationException("Payment was not found for this tenant.");

            if (payment.AccountHolderId != request.AccountHolderId ||
                payment.EnrollmentId != request.EnrollmentId ||
                payment.Amount != request.Amount ||
                payment.PaymentType != request.PaymentType)
            {
                throw new InvalidOperationException("Payment details do not match the requested checkout session.");
            }
        }
        else
        {
            payment = new Payment
            {
                AccountHolderId = request.AccountHolderId,
                EnrollmentId = request.EnrollmentId,
                Amount = request.Amount,
                PaymentDate = DateTime.UtcNow,
                PaymentMethod = StudentRegistrar.Models.PaymentMethod.CreditCard,
                PaymentType = request.PaymentType,
                Notes = "Pending Stripe Checkout session"
            };
            payment.SetPaymentInfo(new PaymentInfo());

            payment = await _paymentRepository.CreateAsync(payment);
        }

        var metadata = new Dictionary<string, string>
        {
            ["tenantId"] = tenant.Id.ToString(),
            ["tenantSubdomain"] = tenant.Subdomain,
            ["paymentId"] = payment.Id.ToString(),
            ["accountHolderId"] = request.AccountHolderId.ToString(),
            ["paymentType"] = request.PaymentType.ToString(),
            ["amount"] = request.Amount.ToString(CultureInfo.InvariantCulture)
        };

        if (request.EnrollmentId.HasValue)
        {
            metadata["enrollmentId"] = request.EnrollmentId.Value.ToString();
        }

        try
        {
            var checkout = await _stripePaymentGateway.CreateConnectedCheckoutSessionAsync(
                new ConnectedCheckoutSessionRequest(
                    tenant.StripeConnectAccountId!,
                    accountHolder.EmailAddress,
                    request.Amount,
                    GetCurrency(),
                    request.SuccessUrl,
                    request.CancelUrl,
                    request.Description ?? $"{request.PaymentType} payment",
                    metadata),
                cancellationToken);

            payment.TransactionId = checkout.SessionId;
            payment.PaymentDate = DateTime.UtcNow;
            payment.Notes = "Pending Stripe Checkout session";
            await _paymentRepository.UpdateAsync(payment);

            return new TenantStripeCheckoutSessionDto
            {
                PaymentId = payment.Id,
                SessionId = checkout.SessionId,
                CheckoutUrl = checkout.CheckoutUrl
            };
        }
        catch
        {
            payment.Notes = "Stripe Checkout session creation failed";
            await _paymentRepository.UpdateAsync(payment);
            throw;
        }
    }

    public async Task<TenantStripeWebhookResultDto> HandleWebhookAsync(
        string payload,
        string signatureHeader,
        CancellationToken cancellationToken = default)
    {
        var webhookSecret = _configuration["Stripe:TenantPaymentsWebhookSecret"]
            ?? _configuration["Stripe:WebhookSecret"];

        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            throw new InvalidOperationException("Stripe webhook secret is not configured.");
        }

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                payload,
                signatureHeader,
                webhookSecret,
                throwOnApiVersionMismatch: false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stripe webhook signature verification failed for tenant payments.");
            throw new InvalidOperationException($"Stripe webhook signature verification failed: {ex.Message}");
        }

        return stripeEvent.Type switch
        {
            "checkout.session.completed" => await HandleCheckoutCompletedAsync(stripeEvent, cancellationToken),
            "payment_intent.succeeded" => await HandlePaymentIntentSucceededAsync(stripeEvent, cancellationToken),
            "payment_intent.payment_failed" => await HandlePaymentIntentFailedAsync(stripeEvent, cancellationToken),
            "charge.refunded" => await HandleChargeRefundedAsync(stripeEvent, cancellationToken),
            _ => new TenantStripeWebhookResultDto
            {
                Success = true,
                Processed = false,
                Message = "Event ignored.",
                EventId = stripeEvent.Id,
                EventType = stripeEvent.Type
            }
        };
    }

    private async Task<TenantStripeWebhookResultDto> HandleCheckoutCompletedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        if (stripeEvent.Data.Object is not Session session)
        {
            return Failed(stripeEvent, "Stripe checkout session payload was invalid.");
        }

        if (!TryGetPaymentId(session.Metadata, out var paymentId))
        {
            return Failed(stripeEvent, "paymentId metadata is missing for checkout.session.completed.");
        }

        var payment = await _paymentRepository.GetByIdAsync(paymentId);
        if (payment is null)
        {
            return Failed(stripeEvent, "Payment was not found for checkout.session.completed.");
        }

        if (payment.Notes?.Contains(SettledMarker, StringComparison.OrdinalIgnoreCase) == true)
        {
            return Succeeded(stripeEvent, "Checkout session was already settled.");
        }

        payment.TransactionId = string.IsNullOrWhiteSpace(session.PaymentIntentId)
            ? payment.TransactionId
            : session.PaymentIntentId;
        payment.PaymentDate = DateTime.UtcNow;
        payment.Notes = $"{SettledMarker} Stripe checkout session completed.";
        await _paymentRepository.UpdateAsync(payment);

        await ApplyEnrollmentSettlementAsync(payment, cancellationToken);

        return Succeeded(stripeEvent, "Checkout session settled.");
    }

    private async Task<TenantStripeWebhookResultDto> HandlePaymentIntentSucceededAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        if (stripeEvent.Data.Object is not PaymentIntent intent)
        {
            return Failed(stripeEvent, "Stripe payment intent payload was invalid.");
        }

        if (!TryGetPaymentId(intent.Metadata, out var paymentId))
        {
            var paymentByTransaction = await _paymentRepository.GetByTransactionIdAsync(intent.Id);
            if (paymentByTransaction is null)
            {
                return Failed(stripeEvent, "Payment was not found for payment_intent.succeeded.");
            }

            return await SettleFromPaymentIntentAsync(stripeEvent, paymentByTransaction, intent, cancellationToken);
        }

        var payment = await _paymentRepository.GetByIdAsync(paymentId);
        if (payment is null)
        {
            return Failed(stripeEvent, "Payment was not found for payment_intent.succeeded.");
        }

        return await SettleFromPaymentIntentAsync(stripeEvent, payment, intent, cancellationToken);
    }

    private async Task<TenantStripeWebhookResultDto> SettleFromPaymentIntentAsync(
        Event stripeEvent,
        Payment payment,
        PaymentIntent intent,
        CancellationToken cancellationToken)
    {
        if (payment.Notes?.Contains(SettledMarker, StringComparison.OrdinalIgnoreCase) == true)
        {
            return Succeeded(stripeEvent, "Payment intent was already settled.");
        }

        payment.TransactionId = intent.Id;
        payment.PaymentDate = DateTime.UtcNow;
        payment.Notes = $"{SettledMarker} Stripe payment intent succeeded.";
        await _paymentRepository.UpdateAsync(payment);

        await ApplyEnrollmentSettlementAsync(payment, cancellationToken);

        return Succeeded(stripeEvent, "Payment intent settled.");
    }

    private async Task<TenantStripeWebhookResultDto> HandlePaymentIntentFailedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        if (stripeEvent.Data.Object is not PaymentIntent intent)
        {
            return Failed(stripeEvent, "Stripe payment intent payload was invalid.");
        }

        Payment? payment = null;

        if (TryGetPaymentId(intent.Metadata, out var paymentId))
        {
            payment = await _paymentRepository.GetByIdAsync(paymentId);
        }

        payment ??= await _paymentRepository.GetByTransactionIdAsync(intent.Id);

        if (payment is null)
        {
            return Failed(stripeEvent, "Payment was not found for payment_intent.payment_failed.");
        }

        payment.TransactionId = intent.Id;
        payment.Notes = $"{FailedMarker} Stripe payment failed.";
        await _paymentRepository.UpdateAsync(payment);

        return Succeeded(stripeEvent, "Payment intent failure recorded.");
    }

    private async Task<TenantStripeWebhookResultDto> HandleChargeRefundedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        if (stripeEvent.Data.Object is not Charge charge)
        {
            return Failed(stripeEvent, "Stripe charge payload was invalid.");
        }

        var existingRefund = await _paymentRepository.GetByTransactionIdAsync(charge.Id);
        if (existingRefund is not null)
        {
            return Succeeded(stripeEvent, "Refund was already recorded.");
        }

        var originalPayment = string.IsNullOrWhiteSpace(charge.PaymentIntentId)
            ? null
            : await _paymentRepository.GetByTransactionIdAsync(charge.PaymentIntentId);

        if (originalPayment is null)
        {
            return Failed(stripeEvent, "Original payment was not found for charge.refunded.");
        }

        var refundAmount = decimal.Round(charge.AmountRefunded / 100m, 2, MidpointRounding.AwayFromZero);
        if (refundAmount <= 0)
        {
            return Succeeded(stripeEvent, "Refund amount was zero; nothing to record.");
        }

        var refundPayment = new Payment
        {
            AccountHolderId = originalPayment.AccountHolderId,
            EnrollmentId = originalPayment.EnrollmentId,
            Amount = refundAmount,
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = StudentRegistrar.Models.PaymentMethod.CreditCard,
            PaymentType = PaymentType.Refund,
            TransactionId = charge.Id,
            Notes = $"{RefundedMarker} Stripe charge refunded for payment {originalPayment.Id}."
        };
        refundPayment.SetPaymentInfo(new PaymentInfo());

        await _paymentRepository.CreateAsync(refundPayment);

        if (originalPayment.EnrollmentId.HasValue)
        {
            var enrollment = await _dbContext.Enrollments.FirstOrDefaultAsync(
                e => e.Id == originalPayment.EnrollmentId.Value,
                cancellationToken);

            if (enrollment is not null)
            {
                enrollment.AmountPaid = Math.Max(0m, enrollment.AmountPaid - refundAmount);
                enrollment.PaymentStatus = ComputeEnrollmentStatus(enrollment.AmountPaid, enrollment.FeeAmount);
                enrollment.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        return Succeeded(stripeEvent, "Refund recorded.");
    }

    private async Task ApplyEnrollmentSettlementAsync(Payment payment, CancellationToken cancellationToken)
    {
        if (!payment.EnrollmentId.HasValue)
        {
            return;
        }

        var enrollment = await _dbContext.Enrollments.FirstOrDefaultAsync(
            e => e.Id == payment.EnrollmentId.Value,
            cancellationToken);

        if (enrollment is null)
        {
            return;
        }

        enrollment.AmountPaid = decimal.Round(enrollment.AmountPaid + payment.Amount, 2, MidpointRounding.AwayFromZero);
        enrollment.PaymentStatus = ComputeEnrollmentStatus(enrollment.AmountPaid, enrollment.FeeAmount);
        enrollment.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static PaymentStatus ComputeEnrollmentStatus(decimal amountPaid, decimal feeAmount)
    {
        if (amountPaid <= 0)
        {
            return PaymentStatus.Pending;
        }

        if (feeAmount <= 0 || amountPaid >= feeAmount)
        {
            return PaymentStatus.Paid;
        }

        return PaymentStatus.Partial;
    }

    private async Task<Tenant> GetCurrentTenantAsync(CancellationToken cancellationToken)
    {
        var tenantId = _tenantContextAccessor.TenantContext?.TenantId
            ?? throw new InvalidOperationException("Tenant context is not available.");

        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Tenant could not be found.");

        return tenant;
    }

    private void EnsureConnectReadyForCollection(Tenant tenant)
    {
        var tenantContext = _tenantContextAccessor.TenantContext;

        if (tenantContext is null || tenantContext.IsSelfHostedMode)
        {
            throw new InvalidOperationException("Tenant-owned Stripe Connect is available in SaaS deployments only.");
        }

        if (!tenantContext.HasPaymentFeatures)
        {
            throw new InvalidOperationException("Stripe Connect is available on paid tiers (Pro and Enterprise).");
        }

        if (!tenant.IsActive)
        {
            throw new InvalidOperationException("Tenant is inactive.");
        }

        if (!_stripePaymentGateway.IsConfigured)
        {
            throw new InvalidOperationException("Stripe Connect is unavailable in this environment.");
        }

        if (string.IsNullOrWhiteSpace(tenant.StripeConnectAccountId))
        {
            throw new InvalidOperationException("Connect a Stripe account before collecting payments.");
        }

        if (!tenant.StripeConnectDetailsSubmitted || !tenant.StripeConnectChargesEnabled || !tenant.StripeConnectPayoutsEnabled)
        {
            throw new InvalidOperationException("Stripe Connect onboarding is incomplete. Complete onboarding and refresh status.");
        }
    }

    private string GetCurrency()
    {
        var configured = _configuration["Stripe:TenantPaymentsCurrency"];
        return string.IsNullOrWhiteSpace(configured) ? "usd" : configured.Trim().ToLowerInvariant();
    }

    private static bool TryGetPaymentId(IDictionary<string, string>? metadata, out Guid paymentId)
    {
        paymentId = Guid.Empty;
        return metadata is not null
            && metadata.TryGetValue("paymentId", out var paymentIdText)
            && Guid.TryParse(paymentIdText, out paymentId);
    }

    private static TenantStripeWebhookResultDto Failed(Event stripeEvent, string message)
    {
        return new TenantStripeWebhookResultDto
        {
            Success = false,
            Processed = true,
            Message = message,
            EventId = stripeEvent.Id,
            EventType = stripeEvent.Type
        };
    }

    private static TenantStripeWebhookResultDto Succeeded(Event stripeEvent, string message)
    {
        return new TenantStripeWebhookResultDto
        {
            Success = true,
            Processed = true,
            Message = message,
            EventId = stripeEvent.Id,
            EventType = stripeEvent.Type
        };
    }
}
