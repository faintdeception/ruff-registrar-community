using System.ComponentModel.DataAnnotations;
using StudentRegistrar.Models;

namespace StudentRegistrar.Api.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PendingEmail { get; set; }
    public DateTime? PendingEmailRequestedAtUtc { get; set; }
    public DateTime? PendingEmailExpiresAtUtc { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public UserRole Role { get; set; }
    public string RoleDisplay => Role.ToString();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    public UserProfileDto? Profile { get; set; }
    
    // Additional properties for authentication
    public string Username { get; set; } = string.Empty;
    public string KeycloakId { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

public class UpdateUserRequest
{
    [EmailAddress]
    [StringLength(320)]
    public string? Email { get; set; }

    [StringLength(100)]
    public string? FirstName { get; set; }
    
    [StringLength(100)]
    public string? LastName { get; set; }
    
    public UserRole? Role { get; set; }
    public bool? IsActive { get; set; }
    public UserProfileDto? Profile { get; set; }
}

public class UserProfileDto
{
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Bio { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

public class RequestEmailChangeRequest
{
    [Required]
    [EmailAddress]
    [StringLength(320)]
    public string NewEmail { get; set; } = string.Empty;
}

public class RequestEmailChangeResponse
{
    public string CurrentEmail { get; set; } = string.Empty;
    public string? PendingEmail { get; set; }
    public DateTime? PendingEmailExpiresAtUtc { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? DebugConfirmationUrl { get; set; }
}

public class ConfirmEmailChangeRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;
}

public class ConfirmEmailChangeResponse
{
    public string Email { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
