using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services.Infrastructure;
using StudentRegistrar.Data;
using StudentRegistrar.Models;

namespace StudentRegistrar.Api.Services;

public sealed class TenantSettingsService : ITenantSettingsService
{
    private readonly StudentRegistrarDbContext _dbContext;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly IPasswordPolicyValidator _passwordPolicyValidator;
    private readonly IDataProtector _dataProtector;
    private readonly ILogger<TenantSettingsService> _logger;

    public TenantSettingsService(
        StudentRegistrarDbContext dbContext,
        ITenantContextAccessor tenantContextAccessor,
        IPasswordPolicyValidator passwordPolicyValidator,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<TenantSettingsService> logger)
    {
        _dbContext = dbContext;
        _tenantContextAccessor = tenantContextAccessor;
        _passwordPolicyValidator = passwordPolicyValidator;
        _dataProtector = dataProtectionProvider.CreateProtector("TenantSettings.InitialInvitePassword");
        _logger = logger;
    }

    public async Task<TenantSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateSettingsAsync(cancellationToken);
        return ToDto(settings);
    }

    public async Task<TenantSettingsDto> UpdateInitialInvitePasswordAsync(UpdateTenantSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _passwordPolicyValidator.ValidateAsync(request.InitialInvitePassword);
        if (!validation.IsValid)
        {
            throw new InsecureInitialInvitePasswordException(validation.FailureReasons);
        }

        if (validation.UsedFallbackBaseline)
        {
            _logger.LogWarning(
                "Validated initial invite password against fallback baseline policy because the " +
                "realm password policy could not be retrieved from Keycloak.");
        }

        var settings = await GetOrCreateSettingsAsync(cancellationToken);
        settings.InitialInvitePasswordEncrypted = _dataProtector.Protect(request.InitialInvitePassword);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(settings);
    }

    public async Task<string> GetValidatedInitialInvitePasswordAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateSettingsAsync(cancellationToken);

        if (string.IsNullOrEmpty(settings.InitialInvitePasswordEncrypted))
        {
            throw new InsecureInitialInvitePasswordException(new[]
            {
                "no initial invite password has been configured yet \u2014 set one in Admin Settings before inviting members"
            });
        }

        string password;
        try
        {
            password = _dataProtector.Unprotect(settings.InitialInvitePasswordEncrypted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt stored initial invite password");
            throw new InsecureInitialInvitePasswordException(new[]
            {
                "the stored initial invite password could not be read \u2014 re-set it in Admin Settings"
            });
        }

        // Defense in depth: re-validate at time of use, not just at save time.
        var validation = await _passwordPolicyValidator.ValidateAsync(password);
        if (!validation.IsValid)
        {
            throw new InsecureInitialInvitePasswordException(validation.FailureReasons);
        }

        return password;
    }

    private async Task<TenantSettings> GetOrCreateSettingsAsync(CancellationToken cancellationToken)
    {
        var tenantId = _tenantContextAccessor.TenantContext?.TenantId
            ?? throw new InvalidOperationException("Tenant context is not available.");

        var settings = await _dbContext.TenantSettings.FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);
        if (settings != null)
        {
            return settings;
        }

        settings = new TenantSettings { TenantId = tenantId };
        _dbContext.TenantSettings.Add(settings);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private static TenantSettingsDto ToDto(TenantSettings settings) => new()
    {
        InitialInvitePasswordConfigured = !string.IsNullOrEmpty(settings.InitialInvitePasswordEncrypted)
    };
}
