using Microsoft.EntityFrameworkCore;
using Stripe;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services.Infrastructure;
using StudentRegistrar.Data;
using StudentRegistrar.Models;

namespace StudentRegistrar.Api.Services;

public interface ITenantPaymentConnectService
{
    Task<TenantPaymentConnectStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<TenantPaymentConnectStatusDto> RefreshStatusAsync(CancellationToken cancellationToken = default);
    Task<TenantPaymentConnectOnboardingLinkDto> CreateOnboardingLinkAsync(CancellationToken cancellationToken = default);
}

public sealed class TenantPaymentConnectService : ITenantPaymentConnectService
{
    private readonly StudentRegistrarDbContext _dbContext;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string? _stripeSecretKey;
    private readonly ILogger<TenantPaymentConnectService> _logger;

    public TenantPaymentConnectService(
        StudentRegistrarDbContext dbContext,
        ITenantContextAccessor tenantContextAccessor,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        ILogger<TenantPaymentConnectService> logger)
    {
        _dbContext = dbContext;
        _tenantContextAccessor = tenantContextAccessor;
        _httpContextAccessor = httpContextAccessor;
        _stripeSecretKey = configuration["Stripe:SecretKey"];
        _logger = logger;
    }

    private bool PlatformStripeConfigured => !string.IsNullOrWhiteSpace(_stripeSecretKey);

    public async Task<TenantPaymentConnectStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var tenant = await GetCurrentTenantAsync(cancellationToken);
        return BuildStatus(tenant);
    }

    public async Task<TenantPaymentConnectStatusDto> RefreshStatusAsync(CancellationToken cancellationToken = default)
    {
        var tenant = await GetCurrentTenantAsync(cancellationToken);
        var tenantContext = _tenantContextAccessor.TenantContext;

        if (!CanUseTenantConnect(tenantContext, tenant, out _))
        {
            return BuildStatus(tenant);
        }

        if (string.IsNullOrWhiteSpace(tenant.StripeConnectAccountId))
        {
            return BuildStatus(tenant);
        }

        try
        {
            var accountService = new AccountService(new StripeClient(_stripeSecretKey));
            var account = await accountService.GetAsync(tenant.StripeConnectAccountId, cancellationToken: cancellationToken);
            ApplyAccountStatus(tenant, account);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh Stripe Connect status for tenant {Subdomain}", tenant.Subdomain);
        }

        return BuildStatus(tenant);
    }

    public async Task<TenantPaymentConnectOnboardingLinkDto> CreateOnboardingLinkAsync(CancellationToken cancellationToken = default)
    {
        var tenant = await GetCurrentTenantAsync(cancellationToken);
        var tenantContext = _tenantContextAccessor.TenantContext;

        if (!CanUseTenantConnect(tenantContext, tenant, out var reason))
        {
            throw new InvalidOperationException(reason ?? "Stripe Connect is not available for this tenant.");
        }

        var stripeClient = new StripeClient(_stripeSecretKey);
        var accountService = new AccountService(stripeClient);
        var accountLinkService = new AccountLinkService(stripeClient);

        if (string.IsNullOrWhiteSpace(tenant.StripeConnectAccountId))
        {
            var account = await accountService.CreateAsync(new AccountCreateOptions
            {
                Type = "standard",
                Email = tenant.AdminEmail,
                Country = "US",
                Metadata = new Dictionary<string, string>
                {
                    ["tenantId"] = tenant.Id.ToString(),
                    ["tenantSubdomain"] = tenant.Subdomain
                }
            }, cancellationToken: cancellationToken);

            tenant.StripeConnectAccountId = account.Id;
            ApplyAccountStatus(tenant, account);
            tenant.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var (refreshUrl, returnUrl) = BuildOnboardingUrls();

        var link = await accountLinkService.CreateAsync(new AccountLinkCreateOptions
        {
            Account = tenant.StripeConnectAccountId,
            Type = "account_onboarding",
            RefreshUrl = refreshUrl,
            ReturnUrl = returnUrl
        }, cancellationToken: cancellationToken);

        return new TenantPaymentConnectOnboardingLinkDto
        {
            Url = link.Url,
            ExpiresAtUtc = link.ExpiresAt
        };
    }

    private async Task<Tenant> GetCurrentTenantAsync(CancellationToken cancellationToken)
    {
        var tenantId = _tenantContextAccessor.TenantContext?.TenantId
            ?? throw new InvalidOperationException("Tenant context is not available.");

        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Tenant could not be found.");

        return tenant;
    }

    private static void ApplyAccountStatus(Tenant tenant, Account account)
    {
        tenant.StripeConnectDetailsSubmitted = account.DetailsSubmitted;
        tenant.StripeConnectChargesEnabled = account.ChargesEnabled;
        tenant.StripeConnectPayoutsEnabled = account.PayoutsEnabled;

        if (tenant.StripeConnectOnboardingCompletedAtUtc is null &&
            tenant.StripeConnectDetailsSubmitted &&
            tenant.StripeConnectChargesEnabled &&
            tenant.StripeConnectPayoutsEnabled)
        {
            tenant.StripeConnectOnboardingCompletedAtUtc = DateTime.UtcNow;
        }

        tenant.UpdatedAt = DateTime.UtcNow;
    }

    private TenantPaymentConnectStatusDto BuildStatus(Tenant tenant)
    {
        var tenantContext = _tenantContextAccessor.TenantContext;
        var isSaaSMode = tenantContext is not null && !tenantContext.IsSelfHostedMode;
        var hasPaymentFeatures = tenantContext?.HasPaymentFeatures ?? false;
        var isConnected = !string.IsNullOrWhiteSpace(tenant.StripeConnectAccountId);

        var isAvailable = CanUseTenantConnect(tenantContext, tenant, out var unavailableReason);

        return new TenantPaymentConnectStatusDto
        {
            IsSaaSMode = isSaaSMode,
            HasPaymentFeatures = hasPaymentFeatures,
            PlatformStripeConfigured = PlatformStripeConfigured,
            IsAvailable = isAvailable,
            IsConnected = isConnected,
            StripeConnectAccountId = tenant.StripeConnectAccountId,
            DetailsSubmitted = tenant.StripeConnectDetailsSubmitted,
            ChargesEnabled = tenant.StripeConnectChargesEnabled,
            PayoutsEnabled = tenant.StripeConnectPayoutsEnabled,
            OnboardingCompletedAtUtc = tenant.StripeConnectOnboardingCompletedAtUtc,
            UnavailableReason = unavailableReason
        };
    }

    private bool CanUseTenantConnect(ITenantContext? tenantContext, Tenant tenant, out string? reason)
    {
        if (tenantContext is null || tenantContext.IsSelfHostedMode)
        {
            reason = "Tenant-owned Stripe Connect is available in SaaS deployments only.";
            return false;
        }

        if (!tenantContext.HasPaymentFeatures)
        {
            reason = "Stripe Connect is available on paid tiers (Pro and Enterprise).";
            return false;
        }

        if (!tenant.IsActive)
        {
            reason = "Tenant is inactive.";
            return false;
        }

        if (!PlatformStripeConfigured)
        {
            reason = "Stripe Connect is unavailable in this environment.";
            return false;
        }

        reason = null;
        return true;
    }

    private (string refreshUrl, string returnUrl) BuildOnboardingUrls()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            throw new InvalidOperationException("Unable to determine request URL for Stripe onboarding.");
        }

        var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
        return (
            $"{baseUrl}/settings/system?stripeConnect=refresh",
            $"{baseUrl}/settings/system?stripeConnect=return"
        );
    }
}
