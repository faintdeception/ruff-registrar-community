using System.ComponentModel.DataAnnotations;
using StudentRegistrar.Models;

namespace StudentRegistrar.Api.DTOs;

public class CreateUserRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    public UserRole Role { get; set; }
    
    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
    
    public UserProfileDto? Profile { get; set; }
}

public class CreateUserResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? TemporaryPassword { get; set; }
    public bool IsTemporary { get; set; }
}
