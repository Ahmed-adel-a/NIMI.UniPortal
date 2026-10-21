namespace NIMI.UniPortal.Modules.Payments.Application.Ledger.GetMyPayments;

/// <summary>One row in the student's payment history (P5.4).</summary>
public sealed record PaymentHistoryItemDto(
    Guid Id,
    string ReceiptNumber,
    Guid FeeItemId,
    string FeeName,
    string FeeNameAr,
    decimal Amount,
    string Method,
    DateTimeOffset PaidAt);
