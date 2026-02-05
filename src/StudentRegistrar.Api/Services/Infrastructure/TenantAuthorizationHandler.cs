using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StudentRegistrar.Data;
using System.Security.Claims;

namespace StudentRegistrar.Api.Services.Infrastructure;

/// <summary>
/// Authorization requirement that ensures the authenticated user belongs to the resolved tenant.
/// This prevents cross-tenant access even if the Host header is spoofed.
/// </summary>
public class TenantMembershipRequirement : IAuthorizationRequirement
{
}

/// <summary>
/// Authorization handler that validates the authenticated user's tenant membership
/// matches the tenant resolved from the request (via subdomain).
/// 
/// Security Model:
/// 1. TenantResolutionMiddleware resolves tenant from subdomain (Host header)
/// 2. JWT authentication validates user identity and extracts claims
/// 3. This handler verifies user's TenantId matches the resolved tenant
/// 4. Access is denied if there's a mismatch, preventing cross-tenant access
/// 
/// This layered approach ensures that even if an attacker spoofs the Host header,
/// they cannot access another tenant's data without also having valid credentials
/// for a user in that tenant.
/// 
/// Performance: User-tenant mappings are cached to avoid database lookups on every request.
/// </summary>
public class TenantAuthorizationHandler : AuthorizationHandler<TenantMembershipRequirement>
{
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ILogger<TenantAuthorizationHandler> _logger;
    private readonly StudentRegistrarDbContext _dbContext;
    private readonly IMemoryCache _cache;
    private const int CacheExpirationMinutes = 5;

    public TenantAuthorizationHandler(
        ITenantContextAccessor tenantContextAccessor,
        ILogger<TenantAuthorizationHandler> logger,
        StudentRegistrarDbContext dbContext,
        IMemoryCache cache)
    {
        _tenantContextAccessor = tenantContextAccessor;
        _logger = logger;
        _dbContext = dbContext;
        _cache = cache;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantMembershipRequirement requirement)
    {
        var tenantContext = _tenantContextAccessor.TenantContext;
        
        // If no tenant context, this is likely a non-tenant route (e.g., portal, health checks)
        // Let it through - route-specific authorization will handle access control
        if (tenantContext == null)
        {
            context.Succeed(requirement);
            return;
        }

        // Self-hosted mode: no tenant isolation needed
        if (tenantContext.IsSelfHostedMode)
        {
            context.Succeed(requirement);
            return;
        }

        // SaaS mode: validate user belongs to the resolved tenant
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Tenant authorization failed: No user ID in claims");
            // Don't call Fail() - let framework handle authorization failure naturally
            return;
        }

        // Look up the user's tenant membership with caching
        // Cache key includes tenant ID to prevent cross-tenant information leakage
        var cacheKey = $"user:tenant:{userId}:{tenantContext.TenantId}";
        var userTenantId = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes);
            
            // We use the Keycloak ID (sub claim) to find the user, then get their TenantId
            // Use projection to only fetch TenantId field for better performance
            var result = await _dbContext.Set<Models.User>()
                .AsNoTracking()
                .Where(u => u.KeycloakId == userId)
                .Select(u => new { u.TenantId })
                .FirstOrDefaultAsync();

            return result?.TenantId;
        });

        if (userTenantId == null)
        {
            _logger.LogWarning(
                "Tenant authorization failed: User {UserId} not found in database", 
                userId);
            // Don't call Fail() - let framework handle authorization failure naturally
            return;
        }

        if (userTenantId != tenantContext.TenantId)
        {
            _logger.LogWarning(
                "Tenant authorization failed: User {UserId} (tenant {UserTenantId}) attempted to access tenant {RequestTenantId}",
                userId,
                userTenantId,
                tenantContext.TenantId);
            // Don't call Fail() - let framework handle authorization failure naturally
            return;
        }

        // User belongs to the resolved tenant - authorization succeeds
        _logger.LogDebug(
            "Tenant authorization succeeded: User {UserId} accessing tenant {TenantId}",
            userId,
            tenantContext.TenantId);
        context.Succeed(requirement);
    }
}

/// <summary>
/// Extension methods for tenant authorization registration.
/// </summary>
public static class TenantAuthorizationExtensions
{
    /// <summary>
    /// Adds tenant authorization services to the DI container.
    /// This includes the authorization handler and policy for tenant membership validation.
    /// </summary>
    public static IServiceCollection AddTenantAuthorization(this IServiceCollection services)
    {
        // Register the authorization handler as scoped (not singleton) because it depends on DbContext
        services.AddScoped<IAuthorizationHandler, TenantAuthorizationHandler>();
        
        // Add authorization policy that requires tenant membership
        services.AddAuthorizationBuilder()
            .AddPolicy("TenantMember", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new TenantMembershipRequirement());
            });
        
        return services;
    }
}
