namespace StudentRegistrar.Api.DTOs;

public sealed class TenantPaymentConnectStatusDto
{
    public bool IsSaaSMode { get; init; }
    public bool HasPaymentFeatures { get; init; }
    public bool PlatformStripeConfigured { get; init; }
    public bool IsAvailable { get; init; }
    public bool IsConnected { get; init; }
    public string? StripeConnectAccountId { get; init; }
    public bool DetailsSubmitted { get; init; }
    public bool ChargesEnabled { get; init; }
    public bool PayoutsEnabled { get; init; }
    public DateTime? OnboardingCompletedAtUtc { get; init; }
    public string? UnavailableReason { get; init; }
}

public sealed class TenantPaymentConnectOnboardingLinkDto
{
    public string Url { get; init; } = string.Empty;
    public DateTime? ExpiresAtUtc { get; init; }
}
