using Microsoft.EntityFrameworkCore;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services.Infrastructure;
using StudentRegistrar.Data;
using StudentRegistrar.Models;

namespace StudentRegistrar.Api.Services;

public sealed class TenantHomeContentService : ITenantHomeContentService
{
    private const int WelcomeTitleMaxLength = 120;
    private const int WelcomeBlurbMaxLength = 600;

    private const string DefaultWelcomeTitle = "Welcome to Student Registrar";
    private const string DefaultWelcomeBlurb = "A comprehensive homeschool management system designed to help you track students, courses, rooms, and educators with ease.";

    private readonly StudentRegistrarDbContext _dbContext;
    private readonly ITenantContextAccessor _tenantContextAccessor;

    public TenantHomeContentService(
        StudentRegistrarDbContext dbContext,
        ITenantContextAccessor tenantContextAccessor)
    {
        _dbContext = dbContext;
        _tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<TenantHomeContentDto> GetHomeContentAsync(CancellationToken cancellationToken = default)
    {
        var tenant = await GetCurrentTenantAsync(cancellationToken);
        return ToDto(tenant);
    }

    public async Task<TenantHomeContentDto> UpdateHomeContentAsync(UpdateTenantHomeContentRequest request, CancellationToken cancellationToken = default)
    {
        var tenant = await GetCurrentTenantAsync(cancellationToken);

        var normalizedTitle = NormalizeOptional(request.WelcomeTitle, WelcomeTitleMaxLength, nameof(request.WelcomeTitle));
        var normalizedBlurb = NormalizeOptional(request.WelcomeBlurb, WelcomeBlurbMaxLength, nameof(request.WelcomeBlurb));

        var theme = tenant.GetTheme();
        theme.HomeWelcomeTitle = normalizedTitle;
        theme.HomeWelcomeBlurb = normalizedBlurb;
        tenant.SetTheme(theme);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(tenant);
    }

    private async Task<Tenant> GetCurrentTenantAsync(CancellationToken cancellationToken)
    {
        var tenantId = _tenantContextAccessor.TenantContext?.TenantId
            ?? throw new InvalidOperationException("Tenant context is not available.");

        return await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Tenant could not be found.");
    }

    private static TenantHomeContentDto ToDto(Tenant tenant)
    {
        var theme = tenant.GetTheme();

        var customTitle = NormalizeOptional(theme.HomeWelcomeTitle, WelcomeTitleMaxLength, null);
        var customBlurb = NormalizeOptional(theme.HomeWelcomeBlurb, WelcomeBlurbMaxLength, null);

        var fallbackTitle = string.IsNullOrWhiteSpace(tenant.Name)
            ? DefaultWelcomeTitle
            : $"Welcome to {tenant.Name}";

        return new TenantHomeContentDto
        {
            WelcomeTitle = customTitle ?? fallbackTitle,
            WelcomeBlurb = customBlurb ?? DefaultWelcomeBlurb,
            HasCustomWelcomeTitle = customTitle is not null,
            HasCustomWelcomeBlurb = customBlurb is not null
        };
    }

    private static string? NormalizeOptional(string? value, int maxLength, string? fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            if (fieldName is null)
            {
                return trimmed[..maxLength];
            }

            throw new ArgumentException($"{fieldName} must be {maxLength} characters or fewer.", fieldName);
        }

        return trimmed;
    }
}
