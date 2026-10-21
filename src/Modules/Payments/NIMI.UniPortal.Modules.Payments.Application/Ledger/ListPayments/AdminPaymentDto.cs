namespace NIMI.UniPortal.Modules.Payments.Application.Ledger.ListPayments;

/// <summary>One payment in the Student Affairs list (P5.3/P5.4).</summary>
public sealed record AdminPaymentDto(
    Guid Id,
    string ReceiptNumber,
    Guid StudentId,
    Guid FeeItemId,
    decimal Amount,
    string Method,
    DateTimeOffset PaidAt);
