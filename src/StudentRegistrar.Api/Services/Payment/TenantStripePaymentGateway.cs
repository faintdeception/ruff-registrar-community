using Stripe;
using Stripe.Checkout;

namespace StudentRegistrar.Api.Services;

public sealed record ConnectedCheckoutSessionRequest(
    string ConnectedAccountId,
    string CustomerEmail,
    decimal Amount,
    string Currency,
    string SuccessUrl,
    string CancelUrl,
    string Description,
    Dictionary<string, string> Metadata);

public sealed record ConnectedCheckoutSessionResponse(string SessionId, string CheckoutUrl, string? PaymentIntentId);

public interface ITenantStripePaymentGateway
{
    bool IsConfigured { get; }
    Task<ConnectedCheckoutSessionResponse> CreateConnectedCheckoutSessionAsync(
        ConnectedCheckoutSessionRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class TenantStripePaymentGateway : ITenantStripePaymentGateway
{
    private readonly string? _secretKey;

    public TenantStripePaymentGateway(IConfiguration configuration)
    {
        _secretKey = configuration["Stripe:SecretKey"];
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_secretKey);

    public async Task<ConnectedCheckoutSessionResponse> CreateConnectedCheckoutSessionAsync(
        ConnectedCheckoutSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Stripe secret key is not configured.");
        }

        var stripeClient = new StripeClient(_secretKey);
        var sessionService = new SessionService(stripeClient);

        var unitAmount = ConvertToMinorUnits(request.Amount);

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = request.SuccessUrl,
            CancelUrl = request.CancelUrl,
            CustomerEmail = request.CustomerEmail,
            ClientReferenceId = request.Metadata.TryGetValue("paymentId", out var paymentId) ? paymentId : null,
            Metadata = request.Metadata,
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = request.Currency,
                        UnitAmount = unitAmount,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = request.Description
                        }
                    }
                }
            },
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                Metadata = request.Metadata
            }
        };

        var stripeRequestOptions = new RequestOptions
        {
            StripeAccount = request.ConnectedAccountId
        };

        var session = await sessionService.CreateAsync(options, stripeRequestOptions, cancellationToken);
        if (string.IsNullOrWhiteSpace(session.Id) || string.IsNullOrWhiteSpace(session.Url))
        {
            throw new InvalidOperationException("Stripe Checkout session was not returned.");
        }

        return new ConnectedCheckoutSessionResponse(session.Id, session.Url, session.PaymentIntentId);
    }

    private static long ConvertToMinorUnits(decimal amount)
    {
        var value = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        return checked((long)(value * 100m));
    }
}
