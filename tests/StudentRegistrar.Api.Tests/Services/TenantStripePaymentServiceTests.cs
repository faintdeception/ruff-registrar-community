using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;
using StudentRegistrar.Api.Services.Infrastructure;
using StudentRegistrar.Data;
using StudentRegistrar.Data.Repositories;
using StudentRegistrar.Models;
using Xunit;

namespace StudentRegistrar.Api.Tests.Services;

public class TenantStripePaymentServiceTests
{
    [Fact]
    public async Task CreateCheckoutSessionAsync_WhenConnectNotReady_ThrowsInvalidOperationException()
    {
        await using var db = CreateDbContext();
        var tenant = SeedTenant(db, t =>
        {
            t.StripeConnectAccountId = null;
            t.StripeConnectDetailsSubmitted = false;
            t.StripeConnectChargesEnabled = false;
            t.StripeConnectPayoutsEnabled = false;
        });
        var accountHolder = SeedAccountHolder(db, tenant.Id);

        var service = CreateService(db, tenant, gatewayConfigured: true);

        var request = BuildCheckoutRequest(accountHolder.Id);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateCheckoutSessionAsync(request));
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_WhenReady_CreatesPendingPaymentAndCheckoutSession()
    {
        await using var db = CreateDbContext();
        var tenant = SeedTenant(db);
        var accountHolder = SeedAccountHolder(db, tenant.Id);

        var gateway = new Mock<ITenantStripePaymentGateway>();
        gateway.SetupGet(g => g.IsConfigured).Returns(true);
        gateway.Setup(g => g.CreateConnectedCheckoutSessionAsync(
                It.IsAny<ConnectedCheckoutSessionRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectedCheckoutSessionResponse(
                "cs_test_123",
                "https://checkout.stripe.com/pay/cs_test_123",
                null));

        var service = CreateService(db, tenant, gateway.Object);

        var result = await service.CreateCheckoutSessionAsync(BuildCheckoutRequest(accountHolder.Id));

        Assert.Equal("cs_test_123", result.SessionId);
        Assert.False(string.IsNullOrWhiteSpace(result.CheckoutUrl));

        var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == result.PaymentId);
        Assert.NotNull(payment);
        Assert.Equal("cs_test_123", payment!.TransactionId);
        Assert.Equal(StudentRegistrar.Models.PaymentMethod.CreditCard, payment.PaymentMethod);

        gateway.Verify(g => g.CreateConnectedCheckoutSessionAsync(
            It.Is<ConnectedCheckoutSessionRequest>(r =>
                r.ConnectedAccountId == tenant.StripeConnectAccountId &&
                r.Amount == 99.50m &&
                r.Metadata.ContainsKey("paymentId")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleWebhookAsync_CheckoutCompleted_SettlesPaymentAndEnrollment()
    {
        await using var db = CreateDbContext();
        var tenant = SeedTenant(db);
        var accountHolder = SeedAccountHolder(db, tenant.Id);
        var enrollment = SeedEnrollment(db, tenant.Id);
        var payment = SeedPayment(db, tenant.Id, accountHolder.Id, enrollment.Id, 50m, "cs_test_123", "Pending Stripe Checkout session");

        var service = CreateService(db, tenant, gatewayConfigured: true);

        var payload = BuildCheckoutSessionCompletedPayload(
            eventId: "evt_checkout_completed",
            sessionId: "cs_test_123",
            paymentIntentId: "pi_test_123",
            paymentId: payment.Id);
        var signature = BuildSignature(payload, "whsec_test");

        var result = await service.HandleWebhookAsync(payload, signature);

        Assert.True(result.Success);

        var updatedPayment = await db.Payments.FirstAsync(p => p.Id == payment.Id);
        var updatedEnrollment = await db.Enrollments.FirstAsync(e => e.Id == enrollment.Id);

        Assert.Equal("pi_test_123", updatedPayment.TransactionId);
        Assert.Contains("[stripe-settled]", updatedPayment.Notes);
        Assert.Equal(50m, updatedEnrollment.AmountPaid);
        Assert.Equal(PaymentStatus.Partial, updatedEnrollment.PaymentStatus);
    }

    [Fact]
    public async Task HandleWebhookAsync_DuplicateCheckoutCompleted_DoesNotDoubleApplyEnrollmentAmount()
    {
        await using var db = CreateDbContext();
        var tenant = SeedTenant(db);
        var accountHolder = SeedAccountHolder(db, tenant.Id);
        var enrollment = SeedEnrollment(db, tenant.Id);
        var payment = SeedPayment(db, tenant.Id, accountHolder.Id, enrollment.Id, 50m, "pi_test_123", "[stripe-settled] Stripe checkout session completed.");
        enrollment.AmountPaid = 50m;
        enrollment.PaymentStatus = PaymentStatus.Partial;
        await db.SaveChangesAsync();

        var service = CreateService(db, tenant, gatewayConfigured: true);

        var payload = BuildCheckoutSessionCompletedPayload(
            eventId: "evt_checkout_completed",
            sessionId: "cs_test_123",
            paymentIntentId: "pi_test_123",
            paymentId: payment.Id);
        var signature = BuildSignature(payload, "whsec_test");

        var result = await service.HandleWebhookAsync(payload, signature);

        Assert.True(result.Success);
        var updatedEnrollment = await db.Enrollments.FirstAsync(e => e.Id == enrollment.Id);
        Assert.Equal(50m, updatedEnrollment.AmountPaid);
    }

    [Fact]
    public async Task HandleWebhookAsync_ChargeRefunded_CreatesRefundPaymentAndAdjustsEnrollment()
    {
        await using var db = CreateDbContext();
        var tenant = SeedTenant(db);
        var accountHolder = SeedAccountHolder(db, tenant.Id);
        var enrollment = SeedEnrollment(db, tenant.Id);
        var payment = SeedPayment(db, tenant.Id, accountHolder.Id, enrollment.Id, 100m, "pi_test_456", "[stripe-settled]");
        enrollment.AmountPaid = 100m;
        enrollment.PaymentStatus = PaymentStatus.Paid;
        await db.SaveChangesAsync();

        var service = CreateService(db, tenant, gatewayConfigured: true);

        var payload = BuildChargeRefundedPayload("evt_refund", "ch_test_456", "pi_test_456", 2500);
        var signature = BuildSignature(payload, "whsec_test");

        var result = await service.HandleWebhookAsync(payload, signature);

        Assert.True(result.Success);

        var refundPayment = await db.Payments.FirstOrDefaultAsync(p => p.TransactionId == "ch_test_456");
        Assert.NotNull(refundPayment);
        Assert.Equal(PaymentType.Refund, refundPayment!.PaymentType);
        Assert.Equal(25.00m, refundPayment.Amount);

        var updatedEnrollment = await db.Enrollments.FirstAsync(e => e.Id == enrollment.Id);
        Assert.Equal(75.00m, updatedEnrollment.AmountPaid);
        Assert.Equal(PaymentStatus.Partial, updatedEnrollment.PaymentStatus);
    }

    private static TenantStripePaymentService CreateService(
        StudentRegistrarDbContext db,
        Tenant tenant,
        bool gatewayConfigured)
    {
        var gateway = new Mock<ITenantStripePaymentGateway>();
        gateway.SetupGet(g => g.IsConfigured).Returns(gatewayConfigured);
        gateway.Setup(g => g.CreateConnectedCheckoutSessionAsync(It.IsAny<ConnectedCheckoutSessionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectedCheckoutSessionResponse("cs_test_123", "https://checkout.stripe.com/pay/cs_test_123", null));

        return CreateService(db, tenant, gateway.Object);
    }

    private static TenantStripePaymentService CreateService(
        StudentRegistrarDbContext db,
        Tenant tenant,
        ITenantStripePaymentGateway gateway)
    {
        var accessor = new TenantContextAccessor
        {
            TenantContext = TenantContext.ForSaaS(tenant)
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Stripe:TenantPaymentsWebhookSecret"] = "whsec_test",
                ["Stripe:TenantPaymentsCurrency"] = "usd"
            })
            .Build();

        var paymentRepo = new PaymentRepository(db);

        return new TenantStripePaymentService(
            db,
            paymentRepo,
            gateway,
            accessor,
            config,
            NullLogger<TenantStripePaymentService>.Instance);
    }

    private static CreateTenantStripeCheckoutSessionDto BuildCheckoutRequest(Guid accountHolderId)
    {
        return new CreateTenantStripeCheckoutSessionDto
        {
            AccountHolderId = accountHolderId,
            Amount = 99.50m,
            PaymentType = PaymentType.CourseFee,
            SuccessUrl = "https://example.com/success",
            CancelUrl = "https://example.com/cancel",
            Description = "Course payment"
        };
    }

    private static StudentRegistrarDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<StudentRegistrarDbContext>()
            .UseInMemoryDatabase($"TenantStripePaymentServiceTests-{Guid.NewGuid()}")
            .Options;

        return new StudentRegistrarDbContext(options, new StaticTenantProvider());
    }

    private static Tenant SeedTenant(StudentRegistrarDbContext db, Action<Tenant>? configure = null)
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Sunrise Homeschool Co-op",
            Subdomain = "sunrise",
            SubscriptionTier = SubscriptionTier.Pro,
            SubscriptionStatus = SubscriptionStatus.Active,
            AdminEmail = "admin@sunrise.local",
            KeycloakRealm = "sunrise-org",
            IsActive = true,
            StripeConnectAccountId = "acct_test_123",
            StripeConnectDetailsSubmitted = true,
            StripeConnectChargesEnabled = true,
            StripeConnectPayoutsEnabled = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        configure?.Invoke(tenant);

        db.Tenants.Add(tenant);
        db.SaveChanges();
        return tenant;
    }

    private static AccountHolder SeedAccountHolder(StudentRegistrarDbContext db, Guid tenantId)
    {
        var accountHolder = new AccountHolder
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FirstName = "Alice",
            LastName = "Parent",
            EmailAddress = "alice.parent@example.com",
            KeycloakUserId = "kc_alice",
            CreatedAt = DateTime.UtcNow.AddDays(-20),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        db.AccountHolders.Add(accountHolder);
        db.SaveChanges();
        return accountHolder;
    }

    private static Enrollment SeedEnrollment(StudentRegistrarDbContext db, Guid tenantId)
    {
        var enrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StudentId = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            SemesterId = Guid.NewGuid(),
            EnrollmentType = EnrollmentType.Enrolled,
            FeeAmount = 120m,
            AmountPaid = 0m,
            PaymentStatus = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        db.Enrollments.Add(enrollment);
        db.SaveChanges();
        return enrollment;
    }

    private static Payment SeedPayment(
        StudentRegistrarDbContext db,
        Guid tenantId,
        Guid accountHolderId,
        Guid? enrollmentId,
        decimal amount,
        string transactionId,
        string notes)
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AccountHolderId = accountHolderId,
            EnrollmentId = enrollmentId,
            Amount = amount,
            PaymentMethod = StudentRegistrar.Models.PaymentMethod.CreditCard,
            PaymentType = PaymentType.CourseFee,
            TransactionId = transactionId,
            Notes = notes,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        payment.SetPaymentInfo(new PaymentInfo());

        db.Payments.Add(payment);
        db.SaveChanges();
        return payment;
    }

    private static string BuildSignature(string payload, string secret)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signedPayload = $"{timestamp}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload))).ToLowerInvariant();
        return $"t={timestamp},v1={signature}";
    }

    private static string BuildCheckoutSessionCompletedPayload(string eventId, string sessionId, string paymentIntentId, Guid paymentId)
    {
        var payload = new
        {
            id = eventId,
            @object = "event",
            api_version = "2025-01-27.acacia",
            account = "acct_test",
            created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            livemode = false,
            pending_webhooks = 1,
            request = new
            {
                id = "req_test",
                idempotency_key = (string?)null
            },
            type = "checkout.session.completed",
            data = new
            {
                @object = new
                {
                    id = sessionId,
                    @object = "checkout.session",
                    status = "complete",
                    mode = "payment",
                    payment_intent = paymentIntentId,
                    metadata = new Dictionary<string, string>
                    {
                        ["paymentId"] = paymentId.ToString()
                    }
                }
            }
        };

        return JsonSerializer.Serialize(payload);
    }

    private static string BuildChargeRefundedPayload(string eventId, string chargeId, string paymentIntentId, long amountRefunded)
    {
        var payload = new
        {
            id = eventId,
            @object = "event",
            api_version = "2025-01-27.acacia",
            account = "acct_test",
            created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            livemode = false,
            pending_webhooks = 1,
            request = new
            {
                id = "req_test",
                idempotency_key = (string?)null
            },
            type = "charge.refunded",
            data = new
            {
                @object = new
                {
                    id = chargeId,
                    @object = "charge",
                    payment_intent = paymentIntentId,
                    amount_refunded = amountRefunded,
                    refunded = true,
                    currency = "usd",
                    paid = true,
                    status = "succeeded"
                }
            }
        };

        return JsonSerializer.Serialize(payload);
    }

    private sealed class StaticTenantProvider : ITenantProvider
    {
        public Guid? CurrentTenantId => null;
        public bool ShouldApplyTenantFilter => false;
    }
}
