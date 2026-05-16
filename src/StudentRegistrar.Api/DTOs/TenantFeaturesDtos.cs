namespace StudentRegistrar.Api.DTOs;

public class TenantFeaturesDto
{
    public string SubscriptionTier { get; set; } = string.Empty;
    public bool IsSelfHostedMode { get; set; }
    public bool HasBranding { get; set; }
    public bool HasPayments { get; set; }
    public bool HasMembershipFees { get; set; }
    public bool HasPrioritySupport { get; set; }
    public List<string> EnabledFeatures { get; set; } = [];
}