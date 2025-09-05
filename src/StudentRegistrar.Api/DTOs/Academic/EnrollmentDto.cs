using System.ComponentModel.DataAnnotations;

namespace StudentRegistrar.Api.DTOs;

public class EnrollmentDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public StudentDto Student { get; set; } = null!;
    public int CourseId { get; set; }
    public CourseDto Course { get; set; } = null!;
    public DateTime EnrollmentDate { get; set; }
    public DateTime? CompletionDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateEnrollmentDto
{
    [Required]
    public int StudentId { get; set; }
    
    [Required]
    public int CourseId { get; set; }
    
    [Required]
    public DateTime EnrollmentDate { get; set; }
    
    [StringLength(20)]
    public string Status { get; set; } = "Active";
}

public class EnrollmentDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string CourseId { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string? CourseCode { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public string EnrollmentType { get; set; } = string.Empty;
    public DateTime EnrollmentDate { get; set; }
    public decimal FeeAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public int? WaitlistPosition { get; set; }
    public string? Notes { get; set; }
}
