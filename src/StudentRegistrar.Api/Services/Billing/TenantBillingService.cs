using Microsoft.EntityFrameworkCore;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services.Infrastructure;
using StudentRegistrar.Data;
using StudentRegistrar.Models;

namespace StudentRegistrar.Api.Services;

public sealed class TenantBillingService : ITenantBillingService
{
    private readonly StudentRegistrarDbContext _dbContext;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ITenantStripeBillingGateway _stripeGateway;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TenantBillingService> _logger;

    public TenantBillingService(
        StudentRegistrarDbContext dbContext,
        ITenantContextAccessor tenantContextAccessor,
        ITenantStripeBillingGateway stripeGateway,
        IConfiguration configuration,
        ILogger<TenantBillingService> logger)
    {
        _dbContext = dbContext;
        _tenantContextAccessor = tenantContextAccessor;
        _stripeGateway = stripeGateway;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<TenantBillingStatusDto> GetCurrentBillingAsync(CancellationToken cancellationToken = default)
    {
        var tenant = await GetCurrentTenantAsync(cancellationToken);
        var tenantContext = _tenantContextAccessor.TenantContext;
        var isSaaSMode = tenantContext is not null && !tenantContext.IsSelfHostedMode;

        if (tenantContext is null || tenantContext.IsSelfHostedMode)
        {
            return CreateStatus(tenant, isSaaSMode, canManageBilling: false, canUndoCancellation: false, unavailableReason: "Billing is managed outside this deployment.");
        }

        if (tenant.IsComplimentary)
        {
            return CreateStatus(tenant, isSaaSMode, canManageBilling: false, canUndoCancellation: false, unavailableReason: "Complimentary access does not use Stripe billing.");
        }

        if (string.IsNullOrWhiteSpace(tenant.StripeSubscriptionId))
        {
            return CreateStatus(tenant, isSaaSMode, canManageBilling: false, canUndoCancellation: false, unavailableReason: "No Stripe subscription is attached to this organization.");
        }

        if (!_stripeGateway.IsConfigured)
        {
            return CreateStatus(tenant, isSaaSMode, canManageBilling: false, canUndoCancellation: false, unavailableReason: "Billing management is unavailable in this environment.");
        }

        var snapshot = await _stripeGateway.GetSubscriptionAsync(tenant.StripeSubscriptionId, cancellationToken);
        var canManageBilling = CanManageBilling(tenant);
        var canUndoCancellation = CanUndoCancellation(tenant);
        return CreateStatus(
            tenant,
            isSaaSMode,
            canManageBilling,
            canUndoCancellation,
            unavailableReason: canManageBilling || canUndoCancellation ? null : "Cancellation is only available for active paid organizations.",
            snapshot);
    }

    public async Task<TenantBillingCancellationDto> ScheduleCancellationAtPeriodEndAsync(CancellationToken cancellationToken = default)
    {
        var tenantContext = _tenantContextAccessor.TenantContext;
        if (tenantContext is null || tenantContext.IsSelfHostedMode)
        {
            throw new InvalidOperationException("Billing is managed outside this deployment.");
        }

        var tenant = await GetCurrentTenantAsync(cancellationToken);

        if (tenant.OffboardingStatus == TenantOffboardingStatus.CancellationScheduled && tenant.AccessEndsAtUtc.HasValue)
        {
            return new TenantBillingCancellationDto
            {
                Subdomain = tenant.Subdomain,
                AccessEndsAtUtc = tenant.AccessEndsAtUtc.Value,
                AlreadyScheduled = true,
                CancellationScheduled = true,
                Message = $"Cancellation is already scheduled. Access ends on {tenant.AccessEndsAtUtc.Value:yyyy-MM-dd HH:mm} UTC."
            };
        }

        if (!CanManageBilling(tenant))
        {
            throw new InvalidOperationException("Cancellation is only available for active paid organizations.");
        }

        if (!_stripeGateway.IsConfigured)
        {
            throw new InvalidOperationException("Billing management is unavailable in this environment.");
        }

        var snapshot = await _stripeGateway.ScheduleCancellationAtPeriodEndAsync(tenant.StripeSubscriptionId!, cancellationToken);
        if (snapshot?.CurrentPeriodEndUtc is not DateTime accessEndsAtUtc)
        {
            throw new InvalidOperationException("Stripe did not return the current billing period end.");
        }

        var retentionDays = Math.Max(0, _configuration.GetValue<int?>("SaaSOffboarding:RetentionDays") ?? 30);
        tenant.OffboardingStatus = TenantOffboardingStatus.CancellationScheduled;
        tenant.OffboardingRequestedAtUtc = DateTime.UtcNow;
        tenant.OffboardingReason = "Tenant admin scheduled in-app cancellation at period end.";
        tenant.AccessEndsAtUtc = accessEndsAtUtc;
        tenant.DeletionScheduledAtUtc = accessEndsAtUtc.AddDays(retentionDays);
        tenant.LastOffboardingAttemptAtUtc = null;
        tenant.LastOffboardingError = null;
        tenant.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Tenant admin scheduled in-app Stripe cancellation for {Subdomain}. Access ends at {AccessEndsAtUtc}",
            tenant.Subdomain,
            accessEndsAtUtc);

        return new TenantBillingCancellationDto
        {
            Subdomain = tenant.Subdomain,
            AccessEndsAtUtc = accessEndsAtUtc,
            AlreadyScheduled = false,
            CancellationScheduled = true,
            Message = $"Cancellation scheduled. Access ends on {accessEndsAtUtc:yyyy-MM-dd HH:mm} UTC."
        };
    }

    public async Task<TenantBillingCancellationDto> UndoScheduledCancellationAsync(CancellationToken cancellationToken = default)
    {
        var tenantContext = _tenantContextAccessor.TenantContext;
        if (tenantContext is null || tenantContext.IsSelfHostedMode)
        {
            throw new InvalidOperationException("Billing is managed outside this deployment.");
        }

        var tenant = await GetCurrentTenantAsync(cancellationToken);

        if (!CanUndoCancellation(tenant))
        {
            throw new InvalidOperationException("No scheduled cancellation is available to undo.");
        }

        if (!_stripeGateway.IsConfigured)
        {
            throw new InvalidOperationException("Billing management is unavailable in this environment.");
        }

        await _stripeGateway.UndoCancellationAtPeriodEndAsync(tenant.StripeSubscriptionId!, cancellationToken);

        tenant.OffboardingStatus = TenantOffboardingStatus.None;
        tenant.OffboardingRequestedAtUtc = null;
        tenant.OffboardingReason = null;
        tenant.AccessEndsAtUtc = null;
        tenant.DeletionScheduledAtUtc = null;
        tenant.LastOffboardingAttemptAtUtc = null;
        tenant.LastOffboardingError = null;
        tenant.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Tenant admin reversed in-app Stripe cancellation for {Subdomain}",
            tenant.Subdomain);

        return new TenantBillingCancellationDto
        {
            Subdomain = tenant.Subdomain,
            AccessEndsAtUtc = null,
            AlreadyScheduled = false,
            CancellationScheduled = false,
            Message = "Scheduled cancellation removed. Billing remains active."
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

    private static TenantBillingStatusDto CreateStatus(
        Tenant tenant,
        bool isSaaSMode,
        bool canManageBilling,
        bool canUndoCancellation,
        string? unavailableReason,
        TenantStripeSubscriptionSnapshot? snapshot = null)
    {
        return new TenantBillingStatusDto
        {
            IsSaaSMode = isSaaSMode,
            CanManageBilling = canManageBilling,
            CanUndoCancellation = canUndoCancellation,
            UnavailableReason = unavailableReason,
            SubscriptionTier = tenant.SubscriptionTier.ToString(),
            SubscriptionStatus = tenant.SubscriptionStatus.ToString(),
            IsComplimentary = tenant.IsComplimentary,
            HasStripeSubscription = !string.IsNullOrWhiteSpace(tenant.StripeSubscriptionId),
            CancelAtPeriodEnd = snapshot?.CancelAtPeriodEnd ?? tenant.OffboardingStatus == TenantOffboardingStatus.CancellationScheduled,
            CurrentPeriodEndUtc = snapshot?.CurrentPeriodEndUtc,
            OffboardingStatus = tenant.OffboardingStatus.ToString(),
            AccessEndsAtUtc = tenant.AccessEndsAtUtc,
            DeletionScheduledAtUtc = tenant.DeletionScheduledAtUtc
        };
    }

    private static bool CanManageBilling(Tenant tenant)
    {
        return tenant.SubscriptionStatus == SubscriptionStatus.Active &&
               tenant.OffboardingStatus == TenantOffboardingStatus.None &&
               !tenant.IsComplimentary &&
               tenant.IsActive &&
               !string.IsNullOrWhiteSpace(tenant.StripeSubscriptionId);
    }

    private static bool CanUndoCancellation(Tenant tenant)
    {
        return tenant.SubscriptionStatus == SubscriptionStatus.Active &&
               tenant.OffboardingStatus == TenantOffboardingStatus.CancellationScheduled &&
               !tenant.IsComplimentary &&
               tenant.IsActive &&
               !string.IsNullOrWhiteSpace(tenant.StripeSubscriptionId);
    }
}