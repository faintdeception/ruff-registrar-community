using System.ComponentModel.DataAnnotations;
using StudentRegistrar.Models;

namespace StudentRegistrar.Api.DTOs;

public class EducatorDto
{
    public Guid Id { get; set; }
    public Guid? CourseId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsPrimary { get; set; } = false;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public EducatorInfo EducatorInfo { get; set; } = new();
    public bool IsAssignedToCourse => CourseId.HasValue;
    public CourseDto? Course { get; set; }
}

public class CreateEducatorDto
{
    public Guid? CourseId { get; set; } // Optional - can create educators without courses
    
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
    
    public bool IsPrimary { get; set; } = false;
    public bool IsActive { get; set; } = true;
    
    public EducatorInfo? EducatorInfo { get; set; }
}

public class UpdateEducatorDto
{
    public Guid? CourseId { get; set; } // Can assign/unassign courses
    
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
    
    public bool IsPrimary { get; set; } = false;
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
