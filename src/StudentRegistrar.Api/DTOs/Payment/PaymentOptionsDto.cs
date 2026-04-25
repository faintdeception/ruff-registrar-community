using System.ComponentModel.DataAnnotations;

namespace StudentRegistrar.Api.DTOs;

public sealed class PaymentOptionsDto
{
    public bool IsSupported { get; set; }
    public bool CanManagePaymentOptions { get; set; }
    public string SubscriptionTier { get; set; } = string.Empty;
    public string? UpgradeMessage { get; set; }
    public bool EnableStripePayments { get; set; }
    public bool HasStripeAccountToken { get; set; }
    public string? StripeAccountTokenPreview { get; set; }
}

public sealed class UpdatePaymentOptionsDto
{
    public bool EnableStripePayments { get; set; }

    [MaxLength(255)]
    public string? StripeAccountToken { get; set; }
}