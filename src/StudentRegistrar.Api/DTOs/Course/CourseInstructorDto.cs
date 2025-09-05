using System.ComponentModel.DataAnnotations;

namespace StudentRegistrar.Api.DTOs;

public class CourseInstructorDto
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Guid? AccountHolderId { get; set; }  // New field for member instructors
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public InstructorInfo InstructorInfo { get; set; } = new();
    public CourseDto? Course { get; set; }
    public AccountHolderDto? AccountHolder { get; set; }  // Navigation property for member instructors
}

public class CreateCourseInstructorDto
{
    [Required]
    public Guid CourseId { get; set; }
    
    // Optional: Link to existing member
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
    
    public bool IsPrimary { get; set; } = false;
    
    public InstructorInfo? InstructorInfo { get; set; }
}

public class UpdateCourseInstructorDto
{
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
    
    public InstructorInfo? InstructorInfo { get; set; }
}

public class InstructorInfo
{
    public string? Bio { get; set; }
    public List<string> Qualifications { get; set; } = new();
    public Dictionary<string, string> CustomFields { get; set; } = new();
}
