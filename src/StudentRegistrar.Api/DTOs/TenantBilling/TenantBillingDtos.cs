namespace StudentRegistrar.Api.DTOs;

public sealed class TenantBillingStatusDto
{
    public bool IsSaaSMode { get; init; }
    public bool CanManageBilling { get; init; }
    public bool CanUndoCancellation { get; init; }
    public string? UnavailableReason { get; init; }
    public string SubscriptionTier { get; init; } = "Free";
    public string SubscriptionStatus { get; init; } = "Unknown";
    public bool IsComplimentary { get; init; }
    public bool HasStripeSubscription { get; init; }
    public bool CancelAtPeriodEnd { get; init; }
    public DateTime? CurrentPeriodEndUtc { get; init; }
    public string OffboardingStatus { get; init; } = "None";
    public DateTime? AccessEndsAtUtc { get; init; }
    public DateTime? DeletionScheduledAtUtc { get; init; }
}

public sealed class TenantBillingCancellationDto
{
    public string Subdomain { get; init; } = string.Empty;
    public DateTime? AccessEndsAtUtc { get; init; }
    public bool AlreadyScheduled { get; init; }
    public bool CancellationScheduled { get; init; }
    public string Message { get; init; } = string.Empty;
}