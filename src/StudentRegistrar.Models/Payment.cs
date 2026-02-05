using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace StudentRegistrar.Models;

public enum PaymentType
{
    CourseFee,
    MembershipDues,
    Refund,
    MaterialsFee,
    LateFee
}

public enum PaymentMethod
{
    Cash,
    Check,
    CreditCard,
    PayPal,
    Venmo,
    Zelle,
    BankTransfer,
    Other
}

public class Payment : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// The tenant (organization) this payment belongs to.
    /// </summary>
    [Required]
    public Guid TenantId { get; set; }
    
    [Required]
    public Guid AccountHolderId { get; set; }
    
    // NULL for membership dues
    public Guid? EnrollmentId { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }
    
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    
    [Required]
    public PaymentMethod PaymentMethod { get; set; }
    
    [Required]
    public PaymentType PaymentType { get; set; }
    
    [MaxLength(255)]
    public string? TransactionId { get; set; }
    
    // Flexible data for payment-specific info
    [Column(TypeName = "jsonb")]
    public string PaymentInfoJson { get; set; } = "{}";
    
    [MaxLength(1000)]
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation Properties
    public virtual AccountHolder AccountHolder { get; set; } = null!;
    public virtual Enrollment? Enrollment { get; set; }
    
    // Helper methods for JSON fields
    public PaymentInfo GetPaymentInfo()
    {
        try
        {
            return JsonSerializer.Deserialize<PaymentInfo>(PaymentInfoJson) ?? new PaymentInfo();
        }
        catch
        {
            return new PaymentInfo();
        }
    }
    
    public void SetPaymentInfo(PaymentInfo info)
    {
        PaymentInfoJson = JsonSerializer.Serialize(info);
    }
}

// Supporting value objects
public class PaymentInfo
{
    public string? CheckNumber { get; set; }
    public string? CardLast4 { get; set; }
    public string? ProcessorResponse { get; set; }
    public Dictionary<string, string> CustomFields { get; set; } = new();
}
