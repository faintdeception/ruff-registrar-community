using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services.Infrastructure;
using StudentRegistrar.Data;
using StudentRegistrar.Models;

namespace StudentRegistrar.Api.Services;

public class PaymentOptionsService : IPaymentOptionsService
{
    private readonly StudentRegistrarDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentOptionsService> _logger;
    private readonly IDataProtector _dataProtector;
    private const string ProtectedTokenPrefix = "dp::";

    public PaymentOptionsService(
        StudentRegistrarDbContext dbContext,
        ITenantContext tenantContext,
        IDataProtectionProvider dataProtectionProvider,
        IConfiguration configuration,
        ILogger<PaymentOptionsService> logger)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _dataProtector = dataProtectionProvider.CreateProtector("TenantPaymentOptions.StripeAccountToken.v1");
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<PaymentOptionsDto> GetCurrentTenantPaymentOptionsAsync(CancellationToken cancellationToken = default)
    {
        var tenant = await GetOrCreateTenantAsync(cancellationToken);
        if (tenant is null)
        {
            return new PaymentOptionsDto
            {
                IsSupported = false,
                CanManagePaymentOptions = false,
                SubscriptionTier = _tenantContext.SubscriptionTier.ToString(),
                UpgradeMessage = "Payment options are not available because the current tenant could not be resolved."
            };
        }

        return MapToDto(tenant);
    }

    public async Task<PaymentOptionsDto> UpdateCurrentTenantPaymentOptionsAsync(
        UpdatePaymentOptionsDto updateDto,
        CancellationToken cancellationToken = default)
    {
        var tenant = await GetOrCreateTenantAsync(cancellationToken);
        if (tenant is null)
        {
            throw new InvalidOperationException("Payment options are not available for the current tenant.");
        }

        var paymentOptions = DecryptPaymentOptions(tenant.GetPaymentOptions(), tenant.Id);
        var hasStoredToken = !string.IsNullOrWhiteSpace(paymentOptions.Stripe.AccountToken);
        var normalizedToken = string.IsNullOrWhiteSpace(updateDto.StripeAccountToken)
            ? null
            : updateDto.StripeAccountToken.Trim();

        if (!_tenantContext.HasPaymentFeatures && updateDto.EnableStripePayments)
        {
            throw new UnauthorizedAccessException("Stripe payments are only available on paid hosted plans.");
        }

        if (updateDto.EnableStripePayments && string.IsNullOrWhiteSpace(normalizedToken) && !hasStoredToken)
        {
            throw new InvalidOperationException("A Stripe account token is required to enable Stripe payments.");
        }

        if (!string.IsNullOrWhiteSpace(normalizedToken))
        {
            paymentOptions.Stripe.AccountToken = normalizedToken;
        }

        paymentOptions.Stripe.Enabled = updateDto.EnableStripePayments;
        tenant.SetPaymentOptions(EncryptPaymentOptions(paymentOptions));

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated payment options for tenant {TenantId}. Stripe enabled: {StripeEnabled}",
            tenant.Id,
            paymentOptions.Stripe.Enabled);

        return MapToDto(tenant);
    }

    private async Task<Tenant?> GetOrCreateTenantAsync(CancellationToken cancellationToken)
    {
        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == _tenantContext.TenantId, cancellationToken);
        if (tenant is not null)
        {
            return tenant;
        }

        if (!_tenantContext.IsSelfHostedMode)
        {
            return null;
        }

        tenant = new Tenant
        {
            Id = _tenantContext.TenantId,
            Name = _configuration["SelfHosted:TenantName"] ?? "Self-Hosted Registrar",
            Subdomain = _configuration["SelfHosted:Subdomain"] ?? "selfhosted",
            SubscriptionTier = SubscriptionTier.Enterprise,
            SubscriptionStatus = SubscriptionStatus.Active,
            KeycloakRealm = _configuration["Keycloak:Realm"] ?? "student-registrar",
            AdminEmail = _configuration["SelfHosted:AdminEmail"] ?? "admin@local.invalid",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created default self-hosted tenant record {TenantId} for payment settings.", tenant.Id);

        return tenant;
    }

    private PaymentOptionsDto MapToDto(Tenant tenant)
    {
        var paymentOptions = DecryptPaymentOptions(tenant.GetPaymentOptions(), tenant.Id);
        var stripeConfigured = !string.IsNullOrWhiteSpace(paymentOptions.Stripe.AccountToken);
        var canManagePaymentOptions = _tenantContext.IsSelfHostedMode || _tenantContext.HasPaymentFeatures || paymentOptions.Stripe.Enabled;

        return new PaymentOptionsDto
        {
            IsSupported = true,
            CanManagePaymentOptions = canManagePaymentOptions,
            SubscriptionTier = tenant.SubscriptionTier.ToString(),
            UpgradeMessage = canManagePaymentOptions || _tenantContext.IsSelfHostedMode
                ? null
                : "Stripe payments are available on paid hosted plans.",
            EnableStripePayments = paymentOptions.Stripe.Enabled,
            HasStripeAccountToken = stripeConfigured,
            StripeAccountTokenPreview = stripeConfigured ? MaskToken(paymentOptions.Stripe.AccountToken!) : null
        };
    }

    private static string MaskToken(string token)
    {
        if (token.Length <= 4)
        {
            return new string('*', token.Length);
        }

        return $"****{token[^4..]}";
    }

    private TenantPaymentOptions EncryptPaymentOptions(TenantPaymentOptions paymentOptions)
    {
        var token = paymentOptions.Stripe.AccountToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            return ClonePaymentOptions(paymentOptions, null);
        }

        if (token.StartsWith(ProtectedTokenPrefix, StringComparison.Ordinal))
        {
            return ClonePaymentOptions(paymentOptions, token);
        }

        var protectedToken = ProtectedTokenPrefix + _dataProtector.Protect(token);
        return ClonePaymentOptions(paymentOptions, protectedToken);
    }

    private TenantPaymentOptions DecryptPaymentOptions(TenantPaymentOptions paymentOptions, Guid tenantId)
    {
        var token = paymentOptions.Stripe.AccountToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            return ClonePaymentOptions(paymentOptions, null);
        }

        if (!token.StartsWith(ProtectedTokenPrefix, StringComparison.Ordinal))
        {
            return ClonePaymentOptions(paymentOptions, token);
        }

        try
        {
            var decryptedToken = _dataProtector.Unprotect(token[ProtectedTokenPrefix.Length..]);
            return ClonePaymentOptions(paymentOptions, decryptedToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to decrypt stored Stripe account token for tenant {TenantId}.", tenantId);
            return ClonePaymentOptions(paymentOptions, null);
        }
    }

    private static TenantPaymentOptions ClonePaymentOptions(TenantPaymentOptions paymentOptions, string? stripeAccountToken)
    {
        return new TenantPaymentOptions
        {
            Stripe = new StripeTenantPaymentOptions
            {
                Enabled = paymentOptions.Stripe.Enabled,
                AccountToken = stripeAccountToken
            }
        };
    }
}