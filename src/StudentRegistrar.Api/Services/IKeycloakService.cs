using StudentRegistrar.Models;
using StudentRegistrar.Api.DTOs;

namespace StudentRegistrar.Api.Services;

public interface IKeycloakService
{
    Task<CreateUserResponse> CreateUserAsync(CreateUserRequest request);
    Task UpdateUserRoleAsync(string keycloakId, UserRole role);
    Task<string?> GetUserIdByEmailAsync(string email);
    Task DeactivateUserAsync(string keycloakId);
    Task<bool> UserExistsAsync(string email);
}
