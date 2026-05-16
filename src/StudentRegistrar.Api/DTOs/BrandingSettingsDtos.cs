using System.ComponentModel.DataAnnotations;

namespace StudentRegistrar.Api.DTOs;

public class BrandingSettingsDto
{
    public string? DisplayName { get; set; }
    public string? LogoBase64 { get; set; }
    public string? LogoMimeType { get; set; }
    public string PrimaryColor { get; set; } = "#3B82F6";
    public string SecondaryColor { get; set; } = "#10B981";
    public string? FooterText { get; set; }
    public bool HidePoweredBy { get; set; }
    public string? CustomCss { get; set; }
    public string SanitizedCustomCss { get; set; } = string.Empty;
}

public class UpdateBrandingSettingsDto
{
    [MaxLength(200)]
    public string? DisplayName { get; set; }

    [MaxLength(700000)]
    public string? LogoBase64 { get; set; }

    [MaxLength(50)]
    public string? LogoMimeType { get; set; }

    [Required]
    [MaxLength(20)]
    public string PrimaryColor { get; set; } = "#3B82F6";

    [Required]
    [MaxLength(20)]
    public string SecondaryColor { get; set; } = "#10B981";

    [MaxLength(200)]
    public string? FooterText { get; set; }

    public bool HidePoweredBy { get; set; }

    [MaxLength(50000)]
    public string? CustomCss { get; set; }
}