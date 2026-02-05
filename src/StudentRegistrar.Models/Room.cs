using System.ComponentModel.DataAnnotations;

namespace StudentRegistrar.Models;

public class Room : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// The tenant (organization) this room belongs to.
    /// </summary>
    [Required]
    public Guid TenantId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [Range(1, 1000)]
    public int Capacity { get; set; }
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    [Required]
    public RoomType RoomType { get; set; } = RoomType.Classroom;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation Properties
    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
}

public enum RoomType
{
    Classroom = 1,
    Lab = 2,
    Auditorium = 3,
    Library = 4,
    Gym = 5,
    Workshop = 6,
    Other = 7
}
