using Microsoft.EntityFrameworkCore;
using StudentRegistrar.Models;
using StudentRegistrar.Data;

namespace StudentRegistrar.Api.Services.Infrastructure;

public sealed class TenantFeatureSnapshot
{
    public required SubscriptionTier SubscriptionTier { get; init; }
    public required bool IsSelfHostedMode { get; init; }
    public required IReadOnlyCollection<string> EnabledFeatures { get; init; }

    public bool HasBranding => EnabledFeatures.Contains(TenantFeature.Branding, StringComparer.OrdinalIgnoreCase);
    public bool HasPayments => EnabledFeatures.Contains(TenantFeature.Payments, StringComparer.OrdinalIgnoreCase);
    public bool HasMembershipFees => EnabledFeatures.Contains(TenantFeature.MembershipFees, StringComparer.OrdinalIgnoreCase);
    public bool HasPrioritySupport => EnabledFeatures.Contains(TenantFeature.PrioritySupport, StringComparer.OrdinalIgnoreCase);
}

public interface ITenantEntitlementService
{
    Task<TenantFeatureSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
    Task<bool> HasFeatureAsync(string featureKey, CancellationToken cancellationToken = default);
}

public class TenantEntitlementService : ITenantEntitlementService
{
    private static readonly string[] FreeFeatures = [];
    private static readonly string[] ProFeatures =
    [
        TenantFeature.Payments,
        TenantFeature.MembershipFees,
    ];
    private static readonly string[] EnterpriseFeatures =
    [
        TenantFeature.Payments,
        TenantFeature.MembershipFees,
        TenantFeature.Branding,
        TenantFeature.PrioritySupport,
    ];

    private readonly StudentRegistrarDbContext _dbContext;
    private readonly ITenantContextAccessor _tenantContextAccessor;

    public TenantEntitlementService(
        StudentRegistrarDbContext dbContext,
        ITenantContextAccessor tenantContextAccessor)
    {
        _dbContext = dbContext;
        _tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<TenantFeatureSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var tenantContext = _tenantContextAccessor.TenantContext;
        if (tenantContext is null)
        {
            return new TenantFeatureSnapshot
            {
                SubscriptionTier = SubscriptionTier.Free,
                IsSelfHostedMode = false,
                EnabledFeatures = []
            };
        }

        var enabledFeatures = new HashSet<string>(GetBaseFeatures(tenantContext.SubscriptionTier), StringComparer.OrdinalIgnoreCase);

        if (tenantContext.IsSelfHostedMode)
        {
            foreach (var feature in TenantFeature.All)
            {
                enabledFeatures.Add(feature);
            }
        }
        else
        {
            var nowUtc = DateTime.UtcNow;
            var overrides = await _dbContext.TenantFeatureOverrides
                .IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantContext.TenantId && (!x.ExpiresAtUtc.HasValue || x.ExpiresAtUtc > nowUtc))
                .ToListAsync(cancellationToken);

            foreach (var featureOverride in overrides)
            {
                if (featureOverride.IsEnabled)
                {
                    enabledFeatures.Add(featureOverride.FeatureKey);
                }
                else
                {
                    enabledFeatures.Remove(featureOverride.FeatureKey);
                }
            }
        }

        return new TenantFeatureSnapshot
        {
            SubscriptionTier = tenantContext.SubscriptionTier,
            IsSelfHostedMode = tenantContext.IsSelfHostedMode,
            EnabledFeatures = enabledFeatures.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray()
        };
    }

    public async Task<bool> HasFeatureAsync(string featureKey, CancellationToken cancellationToken = default)
    {
        var snapshot = await GetSnapshotAsync(cancellationToken);
        return snapshot.EnabledFeatures.Contains(featureKey, StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyCollection<string> GetBaseFeatures(SubscriptionTier tier)
    {
        return tier switch
        {
            SubscriptionTier.Enterprise => EnterpriseFeatures,
            SubscriptionTier.Pro => ProFeatures,
            _ => FreeFeatures,
        };
    }
}