using Microsoft.EntityFrameworkCore;
using StudentRegistrar.Data;
using StudentRegistrar.Models;

namespace StudentRegistrar.Api.Services.Infrastructure;

/// <summary>
/// Middleware that resolves the current tenant from the request.
/// In SaaS mode, extracts subdomain from Host header and looks up tenant.
/// In self-hosted mode, uses a default tenant context.
/// </summary>
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;
    private readonly DeploymentMode _deploymentMode;
    private readonly string _baseDomain;

    public TenantResolutionMiddleware(
        RequestDelegate next,
        ILogger<TenantResolutionMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        
        // Determine deployment mode from environment
        var modeString = configuration["DEPLOYMENT_MODE"] 
            ?? Environment.GetEnvironmentVariable("DEPLOYMENT_MODE") 
            ?? "selfhosted";
        _deploymentMode = modeString.Equals("saas", StringComparison.OrdinalIgnoreCase) 
            ? DeploymentMode.SaaS 
            : DeploymentMode.SelfHosted;
        
        // Base domain for subdomain extraction (e.g., "ruffregistrar.com")
        _baseDomain = configuration["Tenancy:BaseDomain"] ?? "ruffregistrar.com";
        
        _logger.LogInformation("Tenant resolution initialized. Mode: {Mode}, BaseDomain: {BaseDomain}", 
            _deploymentMode, _baseDomain);
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

        // SaaS mode: resolve tenant from subdomain
        var host = context.Request.Host.Host;
        var subdomain = ExtractSubdomain(host);

        if (string.IsNullOrEmpty(subdomain))
        {
            // No subdomain - could be the portal/marketing site or invalid request
            _logger.LogDebug("No subdomain found in host: {Host}", host);
            
            // Allow request to continue for portal routes, controllers will handle appropriately
            tenantContextAccessor.TenantContext = null;
            await _next(context);
            return;
        }

        // Look up tenant by subdomain
        var tenant = await dbContext.Set<Tenant>()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Subdomain == subdomain && t.IsActive);

        if (tenant == null)
        {
            _logger.LogWarning("Tenant not found for subdomain: {Subdomain}", subdomain);
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new { error = "Organization not found", subdomain });
            return;
        }

        if (tenant.SubscriptionStatus == SubscriptionStatus.Cancelled)
        {
            _logger.LogWarning("Tenant subscription cancelled: {Subdomain}", subdomain);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "Subscription cancelled", subdomain });
            return;
        }

        // Set tenant context for the request
        tenantContextAccessor.TenantContext = TenantContext.ForSaaS(tenant);
        _logger.LogDebug("Resolved tenant: {TenantName} ({TenantId}) from subdomain: {Subdomain}", 
            tenant.Name, tenant.Id, subdomain);

        await _next(context);
    }

    /// <summary>
    /// Extracts the subdomain from the host header.
    /// Examples:
    ///   "acme.ruffregistrar.com" -> "acme"
    ///   "www.ruffregistrar.com" -> null (reserved)
    ///   "ruffregistrar.com" -> null
    ///   "localhost:3000" -> null
    /// </summary>
    private string? ExtractSubdomain(string host)
    {
        // Remove port if present
        var hostWithoutPort = host.Split(':')[0];
        
        // Handle localhost for development
        if (hostWithoutPort == "localhost" || hostWithoutPort == "127.0.0.1")
        {
            return null;
        }

        // Check if host ends with base domain
        if (!hostWithoutPort.EndsWith(_baseDomain, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // Extract subdomain
        var baseDomainWithDot = "." + _baseDomain;
        if (hostWithoutPort.Length <= baseDomainWithDot.Length)
        {
            return null; // Just the base domain, no subdomain
        }

        var subdomain = hostWithoutPort[..^baseDomainWithDot.Length].ToLowerInvariant();
        
        // Validate subdomain format
        if (string.IsNullOrEmpty(subdomain) || !IsValidSubdomain(subdomain))
        {
            return null;
        }

        // Reserved subdomains should be treated as "no tenant"
        if (ReservedSubdomains.Contains(subdomain))
        {
            return null;
        }

        return subdomain;
    }

    private static bool IsValidSubdomain(string subdomain)
    {
        // Subdomain rules: 
        // - 1-63 characters
        // - Lowercase alphanumeric and hyphens only
        // - Cannot start or end with hyphen
        if (subdomain.Length < 1 || subdomain.Length > 63)
            return false;
        if (subdomain.StartsWith('-') || subdomain.EndsWith('-'))
            return false;
        return subdomain.All(c => char.IsLetterOrDigit(c) || c == '-');
    }

    /// <summary>
    /// Reserved subdomains that cannot be used by tenants.
    /// </summary>
    private static readonly HashSet<string> ReservedSubdomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "www",
        "api",
        "admin",
        "app",
        "portal",
        "billing",
        "docs",
        "help",
        "support",
        "status",
        "mail",
        "email",
        "smtp",
        "ftp",
        "ssh",
        "vpn",
        "cdn",
        "assets",
        "static",
        "media",
        "images",
        "img",
        "files",
        "download",
        "downloads",
        "blog",
        "news",
        "shop",
        "store",
        "auth",
        "login",
        "signin",
        "signup",
        "register",
        "account",
        "accounts",
        "dashboard",
        "console",
        "panel",
        "test",
        "dev",
        "staging",
        "demo",
        "sandbox"
    };
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
        
        // Register ITenantContext as scoped, resolved from accessor
        services.AddScoped<ITenantContext>(sp =>
        {
            var accessor = sp.GetRequiredService<ITenantContextAccessor>();
            return accessor.TenantContext 
                ?? throw new InvalidOperationException("Tenant context not available. Ensure TenantResolutionMiddleware is configured.");
        });
        
        return services;
    }
}
