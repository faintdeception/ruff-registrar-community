namespace StudentRegistrar.Api.Services.Infrastructure;

public static class ApiSecurityConfigurationValidator
{
    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            return;
        }

        var failures = new List<string>();
        var allowUntrustedCertificates = configuration.GetValue<bool?>("Keycloak:AllowUntrustedCertificates") ?? false;
        var configuredOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        var filteredOrigins = configuredOrigins.Where(origin => !string.IsNullOrWhiteSpace(origin)).ToArray();

        if (allowUntrustedCertificates)
        {
            failures.Add("Keycloak:AllowUntrustedCertificates must be false outside Development");
        }

        if (filteredOrigins.Length == 0)
        {
            failures.Add("Cors:AllowedOrigins must be configured outside Development");
        }

        foreach (var origin in filteredOrigins)
        {
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
                origin.Contains('*', StringComparison.Ordinal))
            {
                failures.Add("Cors:AllowedOrigins entries must be absolute HTTPS origins without wildcards outside Development");
                break;
            }
        }

        RequireHttpsUrl(configuration, failures, "Keycloak:Authority");

        var publicAuthority = configuration["Keycloak:PublicAuthority"];
        if (!string.IsNullOrWhiteSpace(publicAuthority))
        {
            RequireHttpsUrl(configuration, failures, "Keycloak:PublicAuthority");
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException($"API security configuration is invalid: {string.Join("; ", failures)}");
        }
    }

    private static void RequireHttpsUrl(IConfiguration configuration, ICollection<string> failures, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add($"{key} is required");
            return;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            failures.Add($"{key} must be an absolute HTTPS URL");
        }
    }
}