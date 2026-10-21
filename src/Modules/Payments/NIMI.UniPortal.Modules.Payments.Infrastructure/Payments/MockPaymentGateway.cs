using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NIMI.UniPortal.Modules.Payments.Application.Abstractions;
using NIMI.UniPortal.SharedKernel;

namespace NIMI.UniPortal.Modules.Payments.Infrastructure.Payments;

/// <summary>
/// The local mock payment provider (PAYGW). It signs callbacks with HMAC-SHA256 over the raw body and only
/// exists when <c>Payments:Gateway=mock</c>; Production refuses that configuration.
/// </summary>
internal sealed class MockPaymentGateway(PaymentsGatewaySettings settings) : IPaymentGateway
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public string Name => "mock";

    public Task<CheckoutSession> CreateCheckoutAsync(
        CheckoutRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(new CheckoutSession(
            $"mock_{request.PaymentIntentId}",
            new Uri($"/api/payments/mock/{request.PaymentIntentId}", UriKind.Relative)));

    public GatewayCallback ParseAndVerifyCallback(HttpRequestData request)
    {
        if (!request.Headers.TryGetValue("X-Mock-Signature", out var signature)
            || string.IsNullOrWhiteSpace(signature))
            throw new AuthenticationFailedException();

        var expected = Convert.ToHexStringLower(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(settings.MockSecret),
            Encoding.UTF8.GetBytes(request.Body)));

        var matches = CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected),
            Encoding.ASCII.GetBytes(signature.Trim().ToLowerInvariant()));

        if (!matches)
            throw new AuthenticationFailedException();

        try
        {
            return JsonSerializer.Deserialize<GatewayCallback>(request.Body, Json)
                   ?? throw new AuthenticationFailedException();
        }
        catch (JsonException)
        {
            throw new AuthenticationFailedException();
        }
    }
}
