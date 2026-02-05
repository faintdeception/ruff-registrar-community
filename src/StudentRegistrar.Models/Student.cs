using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace StudentRegistrar.Models;

public class Student : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// The tenant (organization) this student belongs to.
    /// </summary>
    [Required]
    public Guid TenantId { get; set; }
    
    [Required]
    public Guid AccountHolderId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string? Grade { get; set; }
    
    public DateTime? DateOfBirth { get; set; }
    
    // Flexible data for special conditions, learning disabilities, etc.
    [Column(TypeName = "jsonb")]
    public string StudentInfoJson { get; set; } = "{}";
    
    [MaxLength(1000)]
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation Properties
    public virtual AccountHolder AccountHolder { get; set; } = null!;
    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    
    // Computed Properties
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";
    
    // Helper methods for JSON fields
    public StudentInfo GetStudentInfo()
    {
        try
        {
            return JsonSerializer.Deserialize<StudentInfo>(StudentInfoJson) ?? new StudentInfo();
        }
        catch
        {
            return new StudentInfo();
        }
    }
    
    public void SetStudentInfo(StudentInfo info)
    {
        StudentInfoJson = JsonSerializer.Serialize(info);
    }
}

// Supporting value objects
public class StudentInfo
{
    public List<string> SpecialConditions { get; set; } = new();
    public List<string> LearningDisabilities { get; set; } = new();
    public List<string> Allergies { get; set; } = new();
    public List<string> Medications { get; set; } = new();
    public string? PreferredName { get; set; }
    public string? ParentNotes { get; set; }
    public string? TeacherNotes { get; set; }
}
