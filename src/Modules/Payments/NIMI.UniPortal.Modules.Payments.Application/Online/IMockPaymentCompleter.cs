namespace NIMI.UniPortal.Modules.Payments.Application.Online;

/// <summary>
/// The mock provider's "complete" action (PAYGW): builds a signed callback and runs it through the real
/// callback handler, so the production path stays under test.
/// </summary>
public interface IMockPaymentCompleter
{
    Task<PaymentIntentDto> CompleteAsync(
        Guid intentId,
        Guid studentId,
        string outcome,
        CancellationToken cancellationToken);
}
