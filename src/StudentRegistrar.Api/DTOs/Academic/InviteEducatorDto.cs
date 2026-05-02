using System.ComponentModel.DataAnnotations;

namespace StudentRegistrar.Api.DTOs;

public class InviteEducatorDto
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Phone { get; set; }

    public Guid? AccountHolderId { get; set; }

    public EducatorInfo? EducatorInfo { get; set; }
}

public class InviteEducatorResponse
{
    public EducatorDto Educator { get; set; } = null!;
    public UserCredentials? Credentials { get; set; }
    public string Message { get; set; } = string.Empty;
}
