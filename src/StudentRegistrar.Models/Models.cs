using System.ComponentModel.DataAnnotations;

namespace StudentRegistrar.Models;

// === LEGACY MODELS REMOVED ===
// LegacyStudent, LegacyCourse, and LegacyEnrollment have been removed.
// Use the new Student, Course, and Enrollment models instead.

public class GradeRecord : ITenantEntity
{
    public int Id { get; set; }
    
    /// <summary>
    /// The tenant (organization) this grade record belongs to.
    /// </summary>
    [Required]
    public Guid TenantId { get; set; }
    
    public Guid StudentId { get; set; }
    public Student Student { get; set; } = null!;
    
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;
    
    [StringLength(10)]
    public string? LetterGrade { get; set; } // A, B, C, D, F
    
    public decimal? NumericGrade { get; set; } // 0-100
    
    public decimal? GradePoints { get; set; } // 0-4.0
    
    [StringLength(500)]
    public string? Comments { get; set; }
    
    public DateTime GradeDate { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AcademicYear : ITenantEntity
{
    public int Id { get; set; }
    
    /// <summary>
    /// The tenant (organization) this academic year belongs to.
    /// </summary>
    [Required]
    public Guid TenantId { get; set; }
    
    [Required]
    [StringLength(20)]
    public string Name { get; set; } = string.Empty; // e.g., "2024-2025"
    
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    
    public bool IsActive { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// User Management Models
public class User : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// The tenant (organization) this user belongs to.
    /// </summary>
    [Required]
    public Guid TenantId { get; set; }
    
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string KeycloakId { get; set; } = string.Empty; // Links to Keycloak user
    public UserRole Role { get; set; } = UserRole.Member;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual UserProfile? UserProfile { get; set; }
}

public enum UserRole
{
    Member = 1,
    Educator = 2,
    Administrator = 3
}

public class UserProfile : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// The tenant (organization) this user profile belongs to.
    /// </summary>
    [Required]
    public Guid TenantId { get; set; }
    
    public Guid UserId { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; } = "US";
    public DateTime? DateOfBirth { get; set; }
    public string? Bio { get; set; }
    public string? ProfilePictureUrl { get; set; }
    
    // Navigation property
    public virtual User User { get; set; } = null!;
}

// ...existing code...