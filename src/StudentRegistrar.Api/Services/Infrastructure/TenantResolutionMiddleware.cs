using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StudentRegistrar.Data;
using StudentRegistrar.Models;

namespace StudentRegistrar.Api.Services.Infrastructure;

/// <summary>
/// Middleware that resolves the current tenant from the request.
/// In SaaS mode, extracts the tenant slug from the request and looks up tenant.
/// In self-hosted mode, uses a default tenant context.
/// </summary>
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;
    private readonly DeploymentMode _deploymentMode;
    private readonly IMemoryCache _cache;
    private const int CacheExpirationMinutes = 5;

    public TenantResolutionMiddleware(
        RequestDelegate next,
        ILogger<TenantResolutionMiddleware> logger,
        IConfiguration configuration,
        IMemoryCache cache)
    {
        _next = next;
        _logger = logger;
        _cache = cache;
        
        // Determine deployment mode from environment
        var modeString = configuration["DEPLOYMENT_MODE"] 
            ?? Environment.GetEnvironmentVariable("DEPLOYMENT_MODE") 
            ?? "selfhosted";
        _deploymentMode = modeString.Equals("saas", StringComparison.OrdinalIgnoreCase) 
            ? DeploymentMode.SaaS 
            : DeploymentMode.SelfHosted;
        
        _logger.LogInformation("Tenant resolution initialized. Mode: {Mode}", _deploymentMode);
    }

    public async Task InvokeAsync(HttpContext context, ITenantContextAccessor tenantContextAccessor, StudentRegistrarDbContext dbContext)
    {
        if (_deploymentMode == DeploymentMode.SelfHosted)
        {
            // Self-hosted mode: use default tenant, skip lookup
            tenantContextAccessor.TenantContext = TenantContext.ForSelfHosted();
            _logger.LogDebug("Self-hosted mode: using default tenant {TenantId}", TenantContext.DefaultTenantId);
            await _next(context);
            return;
        }

        // SaaS mode: resolve tenant from request header or path
        // 
        // SECURITY MODEL (Defense in Depth):
        // ═══════════════════════════════════
        // 1. This middleware extracts tenant from the request header or path
        //    - Request metadata CAN be spoofed by clients
        //    - Tenant slug validation only ensures format correctness
        //    - This establishes the REQUESTED tenant context
        //
        // 2. JWT Authentication validates user identity
        //    - Ensures the user is who they claim to be
        //    - Extracts user claims including Keycloak ID
        //
        // 3. TenantAuthorizationHandler validates tenant membership
        //    - Looks up user's actual TenantId from database
        //    - Compares user's TenantId with the resolved tenant from the request
        //    - Denies access if mismatch (prevents cross-tenant access)
        //
        // This layered approach ensures that even if an attacker spoofs tenant request metadata
        // to request tenant B's subdomain, they cannot access tenant B's data without
        // valid credentials for a user who actually belongs to tenant B.
        //
        // Controllers using [Authorize] will automatically enforce tenant membership
        // via the "TenantMember" policy configured in TenantAuthorizationHandler.
        var tenantSlug = ExtractTenantSlug(context);

        if (string.IsNullOrEmpty(tenantSlug))
        {
            _logger.LogDebug("No tenant slug found on request {Path}", context.Request.Path);
            
            // Allow request to continue for portal routes, controllers will handle appropriately
            tenantContextAccessor.TenantContext = null;
            await _next(context);
            return;
        }

        var cacheKey = $"tenant:slug:{tenantSlug}";
        var tenant = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes);
            
            return await dbContext.Set<Tenant>()
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Subdomain == tenantSlug);
        });

        if (tenant == null)
        {
            _logger.LogWarning("Tenant not found for slug: {TenantSlug}", tenantSlug);
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Organization not found",
                code = "organization_not_found",
                tenant = tenantSlug
            });
            return;
        }

        if (TryGetBlockedTenantReason(tenant, out var blockedReason, out var blockedStatus, out var canRecoverBilling))
        {
            _logger.LogWarning("Tenant access blocked for {TenantSlug}: {Reason}", tenantSlug, blockedReason);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                error = blockedReason,
                code = "tenant_access_blocked",
                tenant = tenantSlug,
                tenantStatus = blockedStatus,
                canRecoverBilling
            });
            return;
        }

        if (tenant.SubscriptionStatus == SubscriptionStatus.Cancelled)
        {
            _logger.LogWarning("Tenant subscription cancelled: {TenantSlug}", tenantSlug);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Subscription cancelled",
                code = "tenant_access_blocked",
                tenant = tenantSlug,
                tenantStatus = "subscription-cancelled",
                canRecoverBilling = false
            });
            return;
        }

        // Set tenant context for the request
        tenantContextAccessor.TenantContext = TenantContext.ForSaaS(tenant);
        _logger.LogDebug("Resolved tenant: {TenantName} ({TenantId}) from slug: {TenantSlug}", 
            tenant.Name, tenant.Id, tenantSlug);

        await _next(context);
    }

    /// <summary>
    /// Extracts the tenant slug from the request metadata.
    /// </summary>
    private static string? ExtractTenantSlug(HttpContext context)
    {
        var explicitHeader = context.Request.Headers["X-Tenant-Slug"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(explicitHeader))
        {
            var normalizedHeader = explicitHeader.Trim().ToLowerInvariant();
            return IsValidTenantSlug(normalizedHeader) ? normalizedHeader : null;
        }

        var segments = context.Request.Path.Value?
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments is null || segments.Length < 2)
        {
            return null;
        }

        if (!string.Equals(segments[0], "org", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var candidate = segments[1].ToLowerInvariant();
        if (!IsValidTenantSlug(candidate))
        {
            return null;
        }

        return candidate;
    }

    private static bool IsValidTenantSlug(string tenantSlug)
    {
        // Tenant slug rules: 
        // - 1-63 characters
        // - Lowercase alphanumeric and hyphens only
        // - Cannot start or end with hyphen
        if (tenantSlug.Length < 1 || tenantSlug.Length > 63)
            return false;
        if (tenantSlug.StartsWith('-') || tenantSlug.EndsWith('-'))
            return false;
        return tenantSlug.All(c => char.IsLetterOrDigit(c) || c == '-');
    }

    private static bool TryGetBlockedTenantReason(Tenant tenant, out string reason, out string status, out bool canRecoverBilling)
    {
        if (tenant.OffboardingStatus == TenantOffboardingStatus.Deleted)
        {
            reason = "Organization has been deleted";
            status = "deleted";
            canRecoverBilling = false;
            return true;
        }

        if (tenant.OffboardingStatus == TenantOffboardingStatus.PendingDeletion)
        {
            reason = "Organization is pending deletion";
            status = "pending-deletion";
            canRecoverBilling = false;
            return true;
        }

        if (tenant.OffboardingStatus == TenantOffboardingStatus.DeletionFailed)
        {
            reason = "Organization access is blocked during deletion retry";
            status = "deletion-failed";
            canRecoverBilling = false;
            return true;
        }

        if (tenant.OffboardingStatus == TenantOffboardingStatus.Suspended)
        {
            reason = "Organization access is suspended";
            status = "suspended";
            canRecoverBilling = true;
            return true;
        }

        if (tenant.OffboardingStatus == TenantOffboardingStatus.CancellationScheduled &&
            tenant.AccessEndsAtUtc.HasValue &&
            tenant.AccessEndsAtUtc.Value <= DateTime.UtcNow)
        {
            reason = "Organization access has ended";
            status = "access-ended";
            canRecoverBilling = true;
            return true;
        }

        if (tenant.SubscriptionStatus == SubscriptionStatus.BillingHold)
        {
            reason = "Billing must be restored before organization access can continue";
            status = "billing-hold";
            canRecoverBilling = true;
            return true;
        }

        if (!tenant.IsActive)
        {
            reason = "Organization access is inactive";
            status = "inactive";
            canRecoverBilling = false;
            return true;
        }

        reason = string.Empty;
        status = string.Empty;
        canRecoverBilling = false;
        return false;
    }

}

/// <summary>
/// Extension methods for tenant middleware registration.
/// </summary>
public static class TenantMiddlewareExtensions
{
    /// <summary>
    /// Adds tenant resolution middleware to the pipeline.
    /// </summary>
    public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TenantResolutionMiddleware>();
    }
    
    /// <summary>
    /// Adds tenant services to the DI container.
    /// </summary>
    public static IServiceCollection AddTenantServices(this IServiceCollection services)
    {
        // Register tenant context accessor as singleton (holds AsyncLocal)
        services.AddSingleton<ITenantContextAccessor, TenantContextAccessor>();
        
        // Register ITenantContext as scoped, resolved from accessor.
        // Returns the current tenant context or throws if not available.
        // Routes that don't require tenant context should use ITenantContextAccessor instead.
        services.AddScoped<ITenantContext>(sp =>
        {
            var accessor = sp.GetRequiredService<ITenantContextAccessor>();
            return accessor.TenantContext 
                ?? throw new InvalidOperationException("Tenant context not available. Ensure TenantResolutionMiddleware is configured and the request has a valid tenant.");
        });
        
        return services;
    }
}
