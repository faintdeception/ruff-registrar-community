using System.ComponentModel.DataAnnotations;
using StudentRegistrar.Models;

namespace StudentRegistrar.Api.DTOs;

public class EducatorDto
{
    public Guid Id { get; set; }
    public Guid? AccountHolderId { get; set; }
    public string? KeycloakUserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public EducatorInfo EducatorInfo { get; set; } = new();
    public AccountHolderDto? AccountHolder { get; set; }
}

public class CreateEducatorDto
{
    public Guid? AccountHolderId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [EmailAddress]
    [StringLength(255)]
    public string? Email { get; set; }
    
    [Phone]
    [StringLength(20)]
    public string? Phone { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public EducatorInfo? EducatorInfo { get; set; }
}

public class UpdateEducatorDto
{
    public Guid? AccountHolderId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [EmailAddress]
    [StringLength(255)]
    public string? Email { get; set; }
    
    [Phone]
    [StringLength(20)]
    public string? Phone { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public EducatorInfo? EducatorInfo { get; set; }
}

public class EducatorInfo
{
    public string? Bio { get; set; }
    public List<string> Qualifications { get; set; } = new();
    public List<string> Specializations { get; set; } = new();
    public string? Department { get; set; }
    public Dictionary<string, string> CustomFields { get; set; } = new();
}
