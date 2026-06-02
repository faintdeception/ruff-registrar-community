using Stripe;

namespace StudentRegistrar.Api.Services;

public sealed record TenantStripeSubscriptionSnapshot(bool CancelAtPeriodEnd, DateTime? CurrentPeriodEndUtc);

public interface ITenantStripeBillingGateway
{
    bool IsConfigured { get; }
    Task<TenantStripeSubscriptionSnapshot?> GetSubscriptionAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default);
    Task<TenantStripeSubscriptionSnapshot?> ScheduleCancellationAtPeriodEndAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default);
    Task<TenantStripeSubscriptionSnapshot?> UndoCancellationAtPeriodEndAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default);
}

public sealed class TenantStripeBillingGateway : ITenantStripeBillingGateway
{
    private readonly string? _secretKey;

    public TenantStripeBillingGateway(IConfiguration configuration)
    {
        _secretKey = configuration["Stripe:SecretKey"];
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_secretKey);

    public async Task<TenantStripeSubscriptionSnapshot?> GetSubscriptionAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(stripeSubscriptionId))
        {
            return null;
        }

        var service = new SubscriptionService(new StripeClient(_secretKey));
        var subscription = await service.GetAsync(stripeSubscriptionId, cancellationToken: cancellationToken);
        return CreateSnapshot(subscription);
    }

    public async Task<TenantStripeSubscriptionSnapshot?> ScheduleCancellationAtPeriodEndAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(stripeSubscriptionId))
        {
            return null;
        }

        var service = new SubscriptionService(new StripeClient(_secretKey));
        var subscription = await service.UpdateAsync(
            stripeSubscriptionId,
            new SubscriptionUpdateOptions
            {
                CancelAtPeriodEnd = true,
            },
            cancellationToken: cancellationToken);

        return CreateSnapshot(subscription);
    }

    public async Task<TenantStripeSubscriptionSnapshot?> UndoCancellationAtPeriodEndAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(stripeSubscriptionId))
        {
            return null;
        }

        var service = new SubscriptionService(new StripeClient(_secretKey));
        var subscription = await service.UpdateAsync(
            stripeSubscriptionId,
            new SubscriptionUpdateOptions
            {
                CancelAtPeriodEnd = false,
            },
            cancellationToken: cancellationToken);

        return CreateSnapshot(subscription);
    }

    private static TenantStripeSubscriptionSnapshot CreateSnapshot(Subscription subscription)
    {
        var currentPeriodEndUtc = subscription.CancelAt == default
            ? subscription.Items?.Data.FirstOrDefault()?.CurrentPeriodEnd
            : subscription.CancelAt;
        return new TenantStripeSubscriptionSnapshot(
            subscription.CancelAtPeriodEnd,
            currentPeriodEndUtc == default ? null : currentPeriodEndUtc);
    }
}