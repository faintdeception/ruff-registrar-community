using System.ComponentModel.DataAnnotations;
using StudentRegistrar.Models;

namespace StudentRegistrar.Api.DTOs;

public class PaymentDto
{
    public string Id { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentType { get; set; } = string.Empty;
    public string? TransactionId { get; set; }
    public string? Notes { get; set; }
}

public class CreatePaymentDto
{
    [Required]
    public Guid AccountHolderId { get; set; }
    
    public Guid? EnrollmentId { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }
    
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    
    [Required]
    public PaymentMethod PaymentMethod { get; set; }
    
    [Required]
    public PaymentType PaymentType { get; set; }
    
    [StringLength(255)]
    public string? TransactionId { get; set; }
    
    [StringLength(1000)]
    public string? Notes { get; set; }
    
    public PaymentInfo? PaymentInfo { get; set; }
}

public class UpdatePaymentDto
{
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal? Amount { get; set; }
    
    public DateTime? PaymentDate { get; set; }
    
    public PaymentMethod? PaymentMethod { get; set; }
    
    public PaymentType? PaymentType { get; set; }
    
    [StringLength(255)]
    public string? TransactionId { get; set; }
    
    [StringLength(1000)]
    public string? Notes { get; set; }
    
    public PaymentInfo? PaymentInfo { get; set; }
}
