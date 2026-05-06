namespace StudentRegistrar.Api.Services.Infrastructure;

public class ApiSecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public ApiSecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";
            headers["Referrer-Policy"] = "no-referrer";
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

            return Task.CompletedTask;
        });

        await _next(context);
    }
}