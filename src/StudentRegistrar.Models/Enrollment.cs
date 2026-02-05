using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace StudentRegistrar.Models;

public enum EnrollmentType
{
    Enrolled,
    Waitlisted,
    Withdrawn,
    Cancelled
}

public enum PaymentStatus
{
    Pending,
    Paid,
    Partial,
    Refunded
}

public class Enrollment : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// The tenant (organization) this enrollment belongs to.
    /// </summary>
    [Required]
    public Guid TenantId { get; set; }
    
    [Required]
    public Guid StudentId { get; set; }
    
    [Required]
    public Guid CourseId { get; set; }
    
    [Required]
    public Guid SemesterId { get; set; }
    
    [Required]
    public EnrollmentType EnrollmentType { get; set; }
    
    public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
    
    // Payment Tracking
    [Column(TypeName = "decimal(10,2)")]
    public decimal FeeAmount { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal AmountPaid { get; set; } = 0;
    
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    
    // Waitlist Management
    public int? WaitlistPosition { get; set; }
    
    // Flexible data for enrollment-specific info
    [Column(TypeName = "jsonb")]
    public string EnrollmentInfoJson { get; set; } = "{}";
    
    [MaxLength(1000)]
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation Properties
    public virtual Student Student { get; set; } = null!;
    public virtual Course Course { get; set; } = null!;
    public virtual Semester Semester { get; set; } = null!;
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    
    // Computed Properties
    [NotMapped]
    public decimal BalanceOwed => FeeAmount - AmountPaid;
    
    [NotMapped]
    public bool IsFullyPaid => AmountPaid >= FeeAmount;
    
    [NotMapped]
    public bool IsWaitlisted => EnrollmentType == EnrollmentType.Waitlisted;
    
    [NotMapped]
    public bool IsActive => EnrollmentType == EnrollmentType.Enrolled;
    
    // Helper methods for JSON fields
    public EnrollmentInfo GetEnrollmentInfo()
    {
        try
        {
            return JsonSerializer.Deserialize<EnrollmentInfo>(EnrollmentInfoJson) ?? new EnrollmentInfo();
        }
        catch
        {
            return new EnrollmentInfo();
        }
    }
    
    public void SetEnrollmentInfo(EnrollmentInfo info)
    {
        EnrollmentInfoJson = JsonSerializer.Serialize(info);
    }
}

// Supporting value objects
public class EnrollmentInfo
{
    public List<string> Accommodations { get; set; } = new();
    public string? SpecialInstructions { get; set; }
    public Dictionary<string, string> CustomFields { get; set; } = new();
    public DateTime? WithdrawalDate { get; set; }
    public string? WithdrawalReason { get; set; }
}
