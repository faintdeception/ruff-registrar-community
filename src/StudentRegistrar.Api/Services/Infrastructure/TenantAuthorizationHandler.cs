using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
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
/// 3. This handler verifies user's TenantId claim matches the resolved tenant
/// 4. Access is denied if there's a mismatch, preventing cross-tenant access
/// 
/// This layered approach ensures that even if an attacker spoofs the Host header,
/// they cannot access another tenant's data without also having valid credentials
/// for a user in that tenant.
/// </summary>
public class TenantAuthorizationHandler : AuthorizationHandler<TenantMembershipRequirement>
{
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ILogger<TenantAuthorizationHandler> _logger;
    private readonly StudentRegistrarDbContext _dbContext;

    public TenantAuthorizationHandler(
        ITenantContextAccessor tenantContextAccessor,
        ILogger<TenantAuthorizationHandler> logger,
        StudentRegistrarDbContext dbContext)
    {
        _tenantContextAccessor = tenantContextAccessor;
        _logger = logger;
        _dbContext = dbContext;
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
            context.Fail();
            return;
        }

        // Look up the user's tenant membership
        // We use the Keycloak ID (sub claim) to find the user, then check their TenantId
        var user = await _dbContext.Set<Models.User>()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.KeycloakId == userId);

        if (user == null)
        {
            _logger.LogWarning(
                "Tenant authorization failed: User {UserId} not found in database", 
                userId);
            context.Fail();
            return;
        }

        if (user.TenantId != tenantContext.TenantId)
        {
            _logger.LogWarning(
                "Tenant authorization failed: User {UserId} (tenant {UserTenantId}) attempted to access tenant {RequestTenantId}",
                userId,
                user.TenantId,
                tenantContext.TenantId);
            context.Fail();
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
        // Register the authorization handler
        services.AddSingleton<IAuthorizationHandler, TenantAuthorizationHandler>();
        
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
