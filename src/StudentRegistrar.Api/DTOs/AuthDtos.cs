namespace StudentRegistrar.Api.DTOs;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthUserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string KeycloakId { get; set; } = string.Empty;
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
}

public class SessionLoginResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public AuthUserDto? User { get; set; }
}

public sealed record KeycloakTokenResponse(
    string AccessToken,
    string? RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset? RefreshTokenExpiresAt);
