using System.Security.Claims;
using Microsoft.Extensions.Options;

namespace StudentRegistrar.Api.Services.Infrastructure;

/// <summary>
/// Keeps the public click-to-try demo accounts read-mostly: they can use the product (courses, enrollments,
/// students, ...) but cannot manage users, billing, payments or tenant settings, which could send email,
/// lock out the demo or touch real money.
/// </summary>
public class DemoAccountGuardMiddleware
{
    private static readonly string[] BlockedPrefixes =
    [
        "/api/tenant-billing",
        "/api/tenant-payment-connect",
        "/api/tenant-stripe-payments",
        "/api/tenant-settings",
        "/api/tenant-home-content",
        "/api/users",
        "/api/accountholders/bulk-import"
    ];

    private const string AllowedUserSyncPath = "/api/users/me/sync";

    private readonly RequestDelegate _next;

    public DemoAccountGuardMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IOptions<DemoLoginOptions> options)
    {
        if (IsBlocked(context, options.Value))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "This action is disabled for the demo account." });
            return;
        }

        await _next(context);
    }

    public static bool IsBlocked(HttpContext context, DemoLoginOptions options)
    {
        if (!options.Enabled
            || HttpMethods.IsGet(context.Request.Method)
            || HttpMethods.IsHead(context.Request.Method)
            || HttpMethods.IsOptions(context.Request.Method)
            || !IsDemoAccount(context.User, options))
        {
            return false;
        }

        var path = context.Request.Path.Value?.TrimEnd('/') ?? string.Empty;
        if (string.Equals(path, AllowedUserSyncPath, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return BlockedPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsDemoAccount(ClaimsPrincipal user, DemoLoginOptions options)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var username = user.FindFirst("preferred_username")?.Value ?? user.Identity.Name;
        if (string.IsNullOrWhiteSpace(username))
        {
            return false;
        }

        return string.Equals(username, options.Admin.Username, StringComparison.OrdinalIgnoreCase)
            || string.Equals(username, options.Member.Username, StringComparison.OrdinalIgnoreCase);
    }
}
