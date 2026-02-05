using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace StudentRegistrar.Models;

public class Educator : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// The tenant (organization) this educator belongs to.
    /// </summary>
    [Required]
    public Guid TenantId { get; set; }
    
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
    
    // Professional status and information
    public bool IsActive { get; set; } = true;
    
    // Optional course assignment - can be null for independent educators
    public Guid? CourseId { get; set; }
    
    // Primary instructor flag - only relevant when assigned to a course
    public bool IsPrimary { get; set; } = false;
    
    // Flexible data for educator-specific info (bio, qualifications, etc.)
    [Column(TypeName = "jsonb")]
    public string EducatorInfoJson { get; set; } = "{}";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation Properties
    public virtual Course? Course { get; set; }
    
    // Computed Properties
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";
    
    [NotMapped]
    public bool IsAssignedToCourse => CourseId.HasValue;
    
    // Helper methods for JSON fields
    public EducatorInfo GetEducatorInfo()
    {
        try
        {
            return JsonSerializer.Deserialize<EducatorInfo>(EducatorInfoJson) ?? new EducatorInfo();
        }
        catch
        {
            return new EducatorInfo();
        }
    }
    
    public void SetEducatorInfo(EducatorInfo info)
    {
        EducatorInfoJson = JsonSerializer.Serialize(info);
    }
}

// Supporting value objects
public class EducatorInfo
{
    public string? Bio { get; set; }
    public List<string> Qualifications { get; set; } = new();
    public List<string> Specializations { get; set; } = new();
    public string? Department { get; set; }
    public Dictionary<string, string> CustomFields { get; set; } = new();
}
