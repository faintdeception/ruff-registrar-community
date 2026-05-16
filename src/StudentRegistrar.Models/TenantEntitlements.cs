using System.ComponentModel.DataAnnotations;

namespace StudentRegistrar.Models;

/// <summary>
/// Well-known tenant feature keys used for entitlement resolution.
/// </summary>
public static class TenantFeature
{
    public const string Branding = "branding";
    public const string Payments = "payments";
    public const string MembershipFees = "membership-fees";
    public const string PrioritySupport = "priority-support";

    public static readonly string[] All =
    [
        Branding,
        Payments,
        MembershipFees,
        PrioritySupport,
    ];
}

/// <summary>
/// Per-tenant feature override applied on top of subscription-tier defaults.
/// </summary>
public class TenantFeatureOverride : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    [Required]
    [MaxLength(100)]
    public string FeatureKey { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Dedicated tenant branding settings for Enterprise-level whitelabeling features.
/// </summary>
public class TenantBrandingSettings : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    [MaxLength(200)]
    public string? DisplayName { get; set; }

    [MaxLength(700000)]
    public string? LogoBase64 { get; set; }

    [MaxLength(50)]
    public string? LogoMimeType { get; set; }

    [MaxLength(20)]
    public string PrimaryColor { get; set; } = "#3B82F6";

    [MaxLength(20)]
    public string SecondaryColor { get; set; } = "#10B981";

    [MaxLength(200)]
    public string? FooterText { get; set; }

    public bool HidePoweredBy { get; set; }

    [MaxLength(50000)]
    public string? CustomCss { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}