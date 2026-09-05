namespace StudentRegistrar.Api.Services;

using StudentRegistrar.Api.DTOs;

public interface ITenantSettingsService
{
    Task<TenantSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates and stores a new initial invite password. Throws
    /// <see cref="InsecureInitialInvitePasswordException"/> if it's blank or fails validation.
    /// </summary>
    Task<TenantSettingsDto> UpdateInitialInvitePasswordAsync(UpdateTenantSettingsRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the tenant's decrypted initial invite password, re-validating it against the
    /// current policy first (defense in depth against stale/directly-edited data). Throws
    /// <see cref="InsecureInitialInvitePasswordException"/> if it's missing or now insecure.
    /// </summary>
    Task<string> GetValidatedInitialInvitePasswordAsync(CancellationToken cancellationToken = default);
}
