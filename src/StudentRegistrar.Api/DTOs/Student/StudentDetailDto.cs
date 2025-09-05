using System.ComponentModel.DataAnnotations;

namespace StudentRegistrar.Api.DTOs;

public class StudentDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Grade { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public StudentInfoDetails StudentInfoJson { get; set; } = new();
    public string? Notes { get; set; }
    public List<EnrollmentDetailDto> Enrollments { get; set; } = new();
}

public class CreateStudentForAccountDto
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [StringLength(20)]
    public string? Grade { get; set; }
    
    public DateTime? DateOfBirth { get; set; }
    public StudentInfoDetails? StudentInfoJson { get; set; }
    public string? Notes { get; set; }
}
