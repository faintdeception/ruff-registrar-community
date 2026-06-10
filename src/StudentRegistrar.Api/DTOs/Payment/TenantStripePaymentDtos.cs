using System.ComponentModel.DataAnnotations;
using StudentRegistrar.Models;

namespace StudentRegistrar.Api.DTOs;

public sealed class CreateTenantStripeCheckoutSessionDto
{
    public Guid? PaymentId { get; init; }

    [Required]
    public Guid AccountHolderId { get; init; }

    public Guid? EnrollmentId { get; init; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; init; }

    [Required]
    public PaymentType PaymentType { get; init; }

    [Required]
    [Url]
    public string SuccessUrl { get; init; } = string.Empty;

    [Required]
    [Url]
    public string CancelUrl { get; init; } = string.Empty;

    [StringLength(200)]
    public string? Description { get; init; }
}

public sealed class TenantStripeCheckoutSessionDto
{
    public Guid PaymentId { get; init; }
    public string SessionId { get; init; } = string.Empty;
    public string CheckoutUrl { get; init; } = string.Empty;
}

public sealed class TenantStripeWebhookResultDto
{
    public bool Success { get; init; }
    public bool Processed { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? EventId { get; init; }
    public string? EventType { get; init; }
}
