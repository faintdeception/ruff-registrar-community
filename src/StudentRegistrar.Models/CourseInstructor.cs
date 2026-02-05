using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace StudentRegistrar.Models;

public class CourseInstructor : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// The tenant (organization) this instructor belongs to.
    /// </summary>
    [Required]
    public Guid TenantId { get; set; }
    
    [Required]
    public Guid CourseId { get; set; }
    
    // Optional link to AccountHolder (for co-op members who are teaching)
    public Guid? AccountHolderId { get; set; }
    
    /// <summary>
    /// Stripe Connect account ID for receiving course payments.
    /// Only used in SaaS mode for paid courses.
    /// </summary>
    [MaxLength(255)]
    public string? StripeAccountId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [EmailAddress]
    [MaxLength(255)]
    public string? Email { get; set; }
    
    [Phone]
    [MaxLength(20)]
    public string? Phone { get; set; }
    
    public bool IsPrimary { get; set; } = false;
    
    // Flexible data for instructor-specific info
    [Column(TypeName = "jsonb")]
    public string InstructorInfoJson { get; set; } = "{}";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation Properties
    public virtual Course Course { get; set; } = null!;
    public virtual AccountHolder? AccountHolder { get; set; }
    
    // Computed Properties
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";
    
    // Helper methods for JSON fields
    public InstructorInfo GetInstructorInfo()
    {
        try
        {
            return JsonSerializer.Deserialize<InstructorInfo>(InstructorInfoJson) ?? new InstructorInfo();
        }
        catch
        {
            return new InstructorInfo();
        }
    }
    
    public void SetInstructorInfo(InstructorInfo info)
    {
        InstructorInfoJson = JsonSerializer.Serialize(info);
    }
}

// Supporting value objects
public class InstructorInfo
{
    public string? Bio { get; set; }
    public List<string> Qualifications { get; set; } = new();
    public Dictionary<string, string> CustomFields { get; set; } = new();
}
