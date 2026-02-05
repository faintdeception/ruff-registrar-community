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
public partial class Tenant
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
        catch (JsonException ex)
        {
            // Note: Using Console.Error as this is a model class without DI access.
            // In production, consider a logging abstraction that can be injected at the service layer.
            Console.Error.WriteLine($"Failed to deserialize TenantTheme from ThemeConfigJson for tenant {Id}: {ex.Message}");
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
public partial class TenantTheme
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
    /// Additional custom CSS. 
    /// SECURITY WARNING: This value MUST be validated and sanitized before rendering
    /// to prevent XSS vulnerabilities. Use the SanitizeCustomCss method before use.
    /// Never render this directly in a style tag without sanitization.
    /// </summary>
    public string? CustomCss { get; set; }
    
    /// <summary>
    /// Sanitizes custom CSS to prevent XSS attacks.
    /// This is a basic sanitizer - consider using a robust CSS parser library for production.
    /// </summary>
    public static string SanitizeCustomCss(string? css)
    {
        if (string.IsNullOrWhiteSpace(css))
            return string.Empty;
        
        // Limit input length to prevent ReDoS attacks
        // Note: C# strings are UTF-16, so 50000 chars ≈ 100KB in memory
        const int maxLength = 50000;
        if (css.Length > maxLength)
            css = css[..maxLength];
            
        // Remove potentially dangerous content using compiled regex patterns
        var sanitized = css;
        
        // Remove script-related content
        sanitized = ScriptTagRegex().Replace(sanitized, "");
            
        // Remove javascript: urls
        sanitized = JavaScriptUrlRegex().Replace(sanitized, "");
            
        // Remove expressions (IE specific)
        sanitized = ExpressionRegex().Replace(sanitized, "");
            
        // Remove import statements (could load external malicious CSS)
        sanitized = ImportRegex().Replace(sanitized, "");
            
        return sanitized;
    }
    
    // Compiled regex patterns for performance and safety
    [System.Text.RegularExpressions.GeneratedRegex(@"<script[^>]*?>.*?</script>", 
        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline, 
        matchTimeoutMilliseconds: 1000)]
    private static partial System.Text.RegularExpressions.Regex ScriptTagRegex();
    
    [System.Text.RegularExpressions.GeneratedRegex(@"javascript\s*:", 
        System.Text.RegularExpressions.RegexOptions.IgnoreCase,
        matchTimeoutMilliseconds: 1000)]
    private static partial System.Text.RegularExpressions.Regex JavaScriptUrlRegex();
    
    [System.Text.RegularExpressions.GeneratedRegex(@"expression\s*\(", 
        System.Text.RegularExpressions.RegexOptions.IgnoreCase,
        matchTimeoutMilliseconds: 1000)]
    private static partial System.Text.RegularExpressions.Regex ExpressionRegex();
    
    [System.Text.RegularExpressions.GeneratedRegex(@"@import", 
        System.Text.RegularExpressions.RegexOptions.IgnoreCase,
        matchTimeoutMilliseconds: 1000)]
    private static partial System.Text.RegularExpressions.Regex ImportRegex();

    /// <summary>
    /// Extensible custom fields
    /// </summary>
    public Dictionary<string, string> CustomFields { get; set; } = new();
}
