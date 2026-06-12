using System.ComponentModel.DataAnnotations;

namespace StudentRegistrar.Models;

/// <summary>
/// Durable processing ledger for Stripe webhook events.
/// Prevents duplicate handling across process restarts and scale-out instances.
/// </summary>
public class ProcessedStripeWebhookEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(255)]
    public string StripeEventId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? StripeSessionId { get; set; }

    /// <summary>
    /// True only after business completion succeeded.
    /// </summary>
    public bool IsCompleted { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAtUtc { get; set; }
}
