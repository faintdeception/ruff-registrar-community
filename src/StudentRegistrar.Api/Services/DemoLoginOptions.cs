namespace StudentRegistrar.Api.Services;

/// <summary>
/// Configuration for the public "click to try" demo login. Disabled unless explicitly enabled
/// and bound to a single tenant subdomain.
/// </summary>
public class DemoLoginOptions
{
    public const string SectionName = "DemoLogin";

    public bool Enabled { get; set; }

    public string? TenantSubdomain { get; set; }

    public DemoLoginAccountOptions Admin { get; set; } = new();

    public DemoLoginAccountOptions Member { get; set; } = new();
}

public class DemoLoginAccountOptions
{
    public string? Username { get; set; }

    /// <summary>
    /// Intentionally public: the account only exists in the demo tenant's realm.
    /// </summary>
    public string? Password { get; set; }

    public bool IsComplete => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
}
