using StudentRegistrar.Models;

namespace StudentRegistrar.Api.Services.Infrastructure;

/// <summary>
/// Deployment mode for the application.
/// Controls multi-tenancy behavior.
/// </summary>
public enum DeploymentMode
{
    /// <summary>
    /// SaaS mode: Multi-tenant with subdomain routing and RLS.
    /// </summary>
    SaaS,
    
    /// <summary>
    /// Self-hosted mode: Single tenant, no isolation needed.
    /// All features enabled, no subscription checks.
    /// </summary>
    SelfHosted
}

/// <summary>
/// Provides access to the current tenant context for the request.
/// In SaaS mode, this is resolved from the subdomain.
/// In self-hosted mode, this returns a default tenant with all features enabled.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// The current tenant ID. All tenant-scoped queries will be filtered by this.
    /// </summary>
    Guid TenantId { get; }
    
    /// <summary>
    /// The current tenant entity, if loaded.
    /// May be null if only TenantId is needed for query filtering.
    /// </summary>
    Tenant? Tenant { get; }
    
    /// <summary>
    /// The current deployment mode.
    /// </summary>
    DeploymentMode DeploymentMode { get; }
    
    /// <summary>
    /// Whether the application is running in SaaS mode.
    /// </summary>
    bool IsSaaSMode => DeploymentMode == DeploymentMode.SaaS;
    
    /// <summary>
    /// Whether the application is running in self-hosted mode.
    /// </summary>
    bool IsSelfHostedMode => DeploymentMode == DeploymentMode.SelfHosted;
    
    /// <summary>
    /// The current subscription tier. In self-hosted mode, always returns Enterprise.
    /// </summary>
    SubscriptionTier SubscriptionTier { get; }
    
    /// <summary>
    /// Whether the current tenant has access to payment features (Pro+ tiers).
    /// </summary>
    bool HasPaymentFeatures => IsSelfHostedMode || SubscriptionTier >= SubscriptionTier.Pro;
    
    /// <summary>
    /// Whether the current tenant has access to branding features (Enterprise tier).
    /// </summary>
    bool HasBrandingFeatures => IsSelfHostedMode || SubscriptionTier >= SubscriptionTier.Enterprise;
}

/// <summary>
/// Mutable tenant context that can be set during request processing.
/// </summary>
public interface ITenantContextAccessor
{
    /// <summary>
    /// Gets or sets the current tenant context.
    /// </summary>
    ITenantContext? TenantContext { get; set; }
}

/// <summary>
/// Implementation of ITenantContextAccessor using AsyncLocal for request scoping.
/// </summary>
public class TenantContextAccessor : ITenantContextAccessor
{
    private static readonly AsyncLocal<TenantContextHolder> _tenantContextCurrent = new();

    public ITenantContext? TenantContext
    {
        get => _tenantContextCurrent.Value?.Context;
        set
        {
            var holder = _tenantContextCurrent.Value;
            if (holder != null)
            {
                // Clear current context trapped in the AsyncLocal
                holder.Context = null;
            }

            if (value != null)
            {
                // Use a holder to allow for clearing in async contexts
                _tenantContextCurrent.Value = new TenantContextHolder { Context = value };
            }
        }
    }

    private class TenantContextHolder
    {
        public ITenantContext? Context;
    }
}

/// <summary>
/// Concrete tenant context implementation.
/// </summary>
public class TenantContext : ITenantContext
{
    public Guid TenantId { get; init; }
    public Tenant? Tenant { get; init; }
    public DeploymentMode DeploymentMode { get; init; }
    public SubscriptionTier SubscriptionTier { get; init; }
    
    /// <summary>
    /// Creates a tenant context for SaaS mode with a specific tenant.
    /// </summary>
    public static TenantContext ForSaaS(Tenant tenant) => new()
    {
        TenantId = tenant.Id,
        Tenant = tenant,
        DeploymentMode = DeploymentMode.SaaS,
        SubscriptionTier = tenant.SubscriptionTier
    };
    
    /// <summary>
    /// Creates a tenant context for self-hosted mode.
    /// Uses a well-known default tenant ID and Enterprise tier.
    /// </summary>
    public static TenantContext ForSelfHosted(Guid? tenantId = null) => new()
    {
        TenantId = tenantId ?? DefaultTenantId,
        Tenant = null,
        DeploymentMode = DeploymentMode.SelfHosted,
        SubscriptionTier = SubscriptionTier.Enterprise // All features enabled
    };
    
    /// <summary>
    /// Well-known tenant ID for self-hosted mode.
    /// </summary>
    public static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
}
