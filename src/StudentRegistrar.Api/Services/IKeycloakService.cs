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

    /// <summary>
    /// Permanently deletes a Keycloak user. Used to clean up a user created moments earlier for a
    /// bulk-import family whose children failed and got rolled back, so a retry doesn't collide
    /// with an orphaned user of the same email. Idempotent — a 404 (already gone) is not an error.
    /// </summary>
    Task DeleteUserAsync(string keycloakId);
    Task<bool> UserExistsAsync(string email);

    /// <summary>
    /// Returns the current realm's raw `passwordPolicy` string (Keycloak's `and`-joined token
    /// format), or null if it could not be retrieved (e.g. admin API unreachable/forbidden).
    /// Callers must not treat null as "no policy" — fall back to a conservative baseline instead.
    /// </summary>
    Task<string?> GetRealmPasswordPolicyAsync();
}
