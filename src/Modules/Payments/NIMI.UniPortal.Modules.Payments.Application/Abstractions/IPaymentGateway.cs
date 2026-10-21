namespace NIMI.UniPortal.Modules.Payments.Application.Abstractions;

/// <summary>
/// The payment-provider seam (decision PAYGW). Implementations live in Infrastructure and translate provider
/// callbacks into the small shape the application layer understands.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>The provider key stored on intents and used in the callback route (<c>mock</c>, <c>paymob</c>, …).</summary>
    string Name { get; }

    Task<CheckoutSession> CreateCheckoutAsync(CheckoutRequest request, CancellationToken cancellationToken);

    /// <summary>Verifies the provider signature and parses the callback; throws on a bad signature.</summary>
    GatewayCallback ParseAndVerifyCallback(HttpRequestData request);
}
