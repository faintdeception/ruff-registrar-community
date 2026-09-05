using StudentRegistrar.Models;
using StudentRegistrar.Api.DTOs;

namespace StudentRegistrar.Api.Services;

public interface IKeycloakService
{
    Task<CreateUserResponse> CreateUserAsync(CreateUserRequest request);
    Task UpdateUserEmailAsync(string keycloakId, string email);
    Task UpdateUserRoleAsync(string keycloakId, UserRole role);
    Task<string?> GetUserIdByEmailAsync(string email);
    Task DeactivateUserAsync(string keycloakId);
    Task<bool> UserExistsAsync(string email);

    /// <summary>
    /// Returns the current realm's raw `passwordPolicy` string (Keycloak's `and`-joined token
    /// format), or null if it could not be retrieved (e.g. admin API unreachable/forbidden).
    /// Callers must not treat null as "no policy" — fall back to a conservative baseline instead.
    /// </summary>
    Task<string?> GetRealmPasswordPolicyAsync();
}
