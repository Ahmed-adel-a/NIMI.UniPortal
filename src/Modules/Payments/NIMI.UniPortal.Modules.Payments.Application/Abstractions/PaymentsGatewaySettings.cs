namespace NIMI.UniPortal.Modules.Payments.Application.Abstractions;

/// <summary>The selected gateway and the mock gateway's signing key (PAYGW; only the mock adapter reads it).</summary>
public sealed record PaymentsGatewaySettings(string Name, string MockSecret)
{
    public bool IsMock => string.Equals(Name, "mock", StringComparison.Ordinal);
}
