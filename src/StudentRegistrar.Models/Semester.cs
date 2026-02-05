using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace StudentRegistrar.Models;

public class Semester : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// The tenant (organization) this semester belongs to.
    /// </summary>
    [Required]
    public Guid TenantId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;
    
    [Required]
    public DateTime StartDate { get; set; }
    
    [Required]
    public DateTime EndDate { get; set; }
    
    public DateTime RegistrationStartDate { get; set; }
    public DateTime RegistrationEndDate { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Flexible period configuration as JSON
    [Column(TypeName = "jsonb")]
    public string PeriodConfigJson { get; set; } = "{}";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation Properties
    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    
    // Computed Properties
    [NotMapped]
    public bool IsRegistrationOpen => 
        RegistrationStartDate <= DateTime.UtcNow && 
        RegistrationEndDate > DateTime.UtcNow;
    
    // Helper methods for JSON fields
    public PeriodConfiguration GetPeriodConfiguration()
    {
        try
        {
            return JsonSerializer.Deserialize<PeriodConfiguration>(PeriodConfigJson) ?? new PeriodConfiguration();
        }
        catch
        {
            return new PeriodConfiguration();
        }
    }
    
    public void SetPeriodConfiguration(PeriodConfiguration config)
    {
        PeriodConfigJson = JsonSerializer.Serialize(config);
    }
}

// Supporting value objects
public class PeriodConfiguration
{
    public List<Period> Periods { get; set; } = new();
    public List<Holiday> Holidays { get; set; } = new();
}

public class Period
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

public class Holiday
{
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? Description { get; set; }
}
