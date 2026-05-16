using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Data;
using StudentRegistrar.Models;

namespace StudentRegistrar.Api.Services.Infrastructure;

public interface IBrandingSettingsService
{
    Task<BrandingSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task<BrandingSettingsDto> UpdateSettingsAsync(UpdateBrandingSettingsDto request, CancellationToken cancellationToken = default);
}

public class BrandingSettingsService : IBrandingSettingsService
{
    private static readonly Regex HexColorRegex = new("^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    private readonly StudentRegistrarDbContext _dbContext;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ITenantEntitlementService _tenantEntitlementService;

    public BrandingSettingsService(
        StudentRegistrarDbContext dbContext,
        ITenantContextAccessor tenantContextAccessor,
        ITenantEntitlementService tenantEntitlementService)
    {
        _dbContext = dbContext;
        _tenantContextAccessor = tenantContextAccessor;
        _tenantEntitlementService = tenantEntitlementService;
    }

    public async Task<BrandingSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureBrandingAccessAsync(cancellationToken);

        var tenantContext = _tenantContextAccessor.TenantContext ?? throw new InvalidOperationException("Tenant context is required.");
        var settings = await _dbContext.TenantBrandingSettings
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantContext.TenantId, cancellationToken);

        return Map(settings);
    }

    public async Task<BrandingSettingsDto> UpdateSettingsAsync(UpdateBrandingSettingsDto request, CancellationToken cancellationToken = default)
    {
        await EnsureBrandingAccessAsync(cancellationToken);
        Validate(request);

        var tenantContext = _tenantContextAccessor.TenantContext ?? throw new InvalidOperationException("Tenant context is required.");
        var settings = await _dbContext.TenantBrandingSettings
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantContext.TenantId, cancellationToken);

        if (settings is null)
        {
            settings = new TenantBrandingSettings
            {
                TenantId = tenantContext.TenantId,
                CreatedAt = DateTime.UtcNow,
            };
            _dbContext.TenantBrandingSettings.Add(settings);
        }

        settings.DisplayName = Normalize(request.DisplayName);
        settings.LogoBase64 = Normalize(request.LogoBase64);
        settings.LogoMimeType = Normalize(request.LogoMimeType);
        settings.PrimaryColor = request.PrimaryColor.ToUpperInvariant();
        settings.SecondaryColor = request.SecondaryColor.ToUpperInvariant();
        settings.FooterText = Normalize(request.FooterText);
        settings.HidePoweredBy = request.HidePoweredBy;
        settings.CustomCss = request.CustomCss;
        settings.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Map(settings);
    }

    private async Task EnsureBrandingAccessAsync(CancellationToken cancellationToken)
    {
        if (!await _tenantEntitlementService.HasFeatureAsync(TenantFeature.Branding, cancellationToken))
        {
            throw new UnauthorizedAccessException("Branding is not enabled for this tenant.");
        }
    }

    private static void Validate(UpdateBrandingSettingsDto request)
    {
        if (!HexColorRegex.IsMatch(request.PrimaryColor))
        {
            throw new InvalidOperationException("Primary color must be a 6-digit hex color such as #3B82F6.");
        }

        if (!HexColorRegex.IsMatch(request.SecondaryColor))
        {
            throw new InvalidOperationException("Secondary color must be a 6-digit hex color such as #10B981.");
        }

        if (!string.IsNullOrWhiteSpace(request.LogoBase64)
            && string.IsNullOrWhiteSpace(request.LogoMimeType))
        {
            throw new InvalidOperationException("Logo MIME type is required when a logo is provided.");
        }
    }

    private static BrandingSettingsDto Map(TenantBrandingSettings? settings)
    {
        var customCss = settings?.CustomCss;
        return new BrandingSettingsDto
        {
            DisplayName = settings?.DisplayName,
            LogoBase64 = settings?.LogoBase64,
            LogoMimeType = settings?.LogoMimeType,
            PrimaryColor = settings?.PrimaryColor ?? "#3B82F6",
            SecondaryColor = settings?.SecondaryColor ?? "#10B981",
            FooterText = settings?.FooterText,
            HidePoweredBy = settings?.HidePoweredBy ?? false,
            CustomCss = customCss,
            SanitizedCustomCss = TenantTheme.SanitizeCustomCss(customCss),
        };
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}