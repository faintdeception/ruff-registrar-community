using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace StudentRegistrar.Models;

public class Course : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// The tenant (organization) this course belongs to.
    /// </summary>
    [Required]
    public Guid TenantId { get; set; }
    
    [Required]
    public Guid SemesterId { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? Code { get; set; }
    
    public string? Description { get; set; }
    
    // Room relationship - replaces old string Room field
    public Guid? RoomId { get; set; }
    
    [Required]
    public int MaxCapacity { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal Fee { get; set; } = 0;
    
    // Flexible period and time configuration
    [MaxLength(50)]
    public string? PeriodCode { get; set; }
    
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    
    // Flexible configuration for course specifics
    [Column(TypeName = "jsonb")]
    public string CourseConfigJson { get; set; } = "{}";
    
    [Required]
    [MaxLength(100)]
    public string AgeGroup { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation Properties
    public virtual Semester Semester { get; set; } = null!;
    public virtual Room? Room { get; set; }
    public virtual ICollection<CourseInstructor> CourseInstructors { get; set; } = new List<CourseInstructor>();
    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    
    // Computed Properties
    [NotMapped]
    public int CurrentEnrollment => Enrollments.Count(e => e.EnrollmentType == EnrollmentType.Enrolled);
    
    [NotMapped]
    public int AvailableSpots => MaxCapacity - CurrentEnrollment;
    
    [NotMapped]
    public bool IsFull => CurrentEnrollment >= MaxCapacity;
    
    [NotMapped]
    public string TimeSlot => 
        StartTime.HasValue && EndTime.HasValue 
            ? $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}" 
            : "Time TBD";
    
    // Helper methods for JSON fields
    public CourseConfiguration GetCourseConfiguration()
    {
        try
        {
            return JsonSerializer.Deserialize<CourseConfiguration>(CourseConfigJson) ?? new CourseConfiguration();
        }
        catch
        {
            return new CourseConfiguration();
        }
    }
    
    public void SetCourseConfiguration(CourseConfiguration config)
    {
        CourseConfigJson = JsonSerializer.Serialize(config);
    }
}

// Supporting value objects
public class CourseConfiguration
{
    public List<string> Prerequisites { get; set; } = new();
    public List<string> Materials { get; set; } = new();
    public List<string> DaysOfWeek { get; set; } = new();
    public string? GradeRange { get; set; }
    public Dictionary<string, string> CustomFields { get; set; } = new();
}
