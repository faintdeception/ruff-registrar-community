using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace StudentRegistrar.Models;

/// <summary>
/// Subscription tier for SaaS billing.
/// Free: Core features, no payment processing
/// Pro: Stripe payments enabled ($19/mo)
/// Enterprise: Pro + logo, theming, whitelabeling ($49/mo)
/// </summary>
public enum SubscriptionTier
{
    Free = 0,
    Pro = 1,
    Enterprise = 2
}

/// <summary>
/// Status of a tenant's subscription.
/// </summary>
public enum SubscriptionStatus
{
    Active = 0,
    PastDue = 1,
    Cancelled = 2,
    Trialing = 3
}

/// <summary>
/// Represents an organization (tenant) in the multi-tenant SaaS system.
/// In self-hosted mode, there is typically one tenant or tenant logic is bypassed.
/// </summary>
public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Display name of the organization (e.g., "Sunrise Homeschool Co-op")
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Subdomain for the organization (e.g., "sunrise" for sunrise.ruffregistrar.com).
    /// Must be unique, lowercase, alphanumeric with hyphens.
    /// </summary>
    [Required]
    [MaxLength(63)] // DNS subdomain limit
    public string Subdomain { get; set; } = string.Empty;

    /// <summary>
    /// Current subscription tier (Free/Pro/Enterprise).
    /// </summary>
    public SubscriptionTier SubscriptionTier { get; set; } = SubscriptionTier.Free;

    /// <summary>
    /// Current subscription status.
    /// </summary>
    public SubscriptionStatus SubscriptionStatus { get; set; } = SubscriptionStatus.Active;

    /// <summary>
    /// Stripe Customer ID for platform billing (subscription to ruff-registrar).
    /// </summary>
    [MaxLength(255)]
    public string? StripeCustomerId { get; set; }

    /// <summary>
    /// Stripe Subscription ID for the platform subscription.
    /// </summary>
    [MaxLength(255)]
    public string? StripeSubscriptionId { get; set; }

    /// <summary>
    /// Logo image data stored as base64 (Enterprise tier only).
    /// Limited to ~500KB to keep DB manageable.
    /// </summary>
    [MaxLength(700000)] // ~500KB base64
    public string? LogoBase64 { get; set; }

    /// <summary>
    /// Logo MIME type (e.g., "image/png", "image/svg+xml").
    /// </summary>
    [MaxLength(50)]
    public string? LogoMimeType { get; set; }

    /// <summary>
    /// Theme configuration as JSON (Enterprise tier only).
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string ThemeConfigJson { get; set; } = "{}";

    /// <summary>
    /// Keycloak realm name for this tenant.
    /// Typically matches subdomain (e.g., "sunrise-org").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string KeycloakRealm { get; set; } = string.Empty;

    /// <summary>
    /// Primary contact email for the organization admin.
    /// </summary>
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string AdminEmail { get; set; } = string.Empty;

    /// <summary>
    /// Whether the tenant is active and can be accessed.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Helper methods for theme configuration
    public TenantTheme GetTheme()
    {
        try
        {
            return JsonSerializer.Deserialize<TenantTheme>(ThemeConfigJson) ?? new TenantTheme();
        }
        catch
        {
            return new TenantTheme();
        }
    }

    public void SetTheme(TenantTheme theme)
    {
        ThemeConfigJson = JsonSerializer.Serialize(theme);
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Theme configuration for Enterprise tier whitelabeling.
/// </summary>
public class TenantTheme
{
    /// <summary>
    /// Primary brand color (hex, e.g., "#3B82F6")
    /// </summary>
    public string PrimaryColor { get; set; } = "#3B82F6";

    /// <summary>
    /// Secondary brand color
    /// </summary>
    public string SecondaryColor { get; set; } = "#10B981";

    /// <summary>
    /// Custom organization display name (overrides Name if set)
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Custom footer text
    /// </summary>
    public string? FooterText { get; set; }

    /// <summary>
    /// Hide "Powered by Ruff Registrar" branding
    /// </summary>
    public bool HidePoweredBy { get; set; } = false;

    /// <summary>
    /// Additional custom CSS (validated/sanitized before use)
    /// </summary>
    public string? CustomCss { get; set; }

    /// <summary>
    /// Extensible custom fields
    /// </summary>
    public Dictionary<string, string> CustomFields { get; set; } = new();
}
