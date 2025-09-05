using System.ComponentModel.DataAnnotations;

namespace StudentRegistrar.Api.DTOs;

public class GradeRecordDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public StudentDto Student { get; set; } = null!;
    public int CourseId { get; set; }
    public CourseDto Course { get; set; } = null!;
    public string? LetterGrade { get; set; }
    public decimal? NumericGrade { get; set; }
    public decimal? GradePoints { get; set; }
    public string? Comments { get; set; }
    public DateTime GradeDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateGradeRecordDto
{
    [Required]
    public int StudentId { get; set; }
    
    [Required]
    public int CourseId { get; set; }
    
    [StringLength(10)]
    public string? LetterGrade { get; set; }
    
    [Range(0, 100)]
    public decimal? NumericGrade { get; set; }
    
    [Range(0, 4)]
    public decimal? GradePoints { get; set; }
    
    [StringLength(500)]
    public string? Comments { get; set; }
    
    [Required]
    public DateTime GradeDate { get; set; }
}
