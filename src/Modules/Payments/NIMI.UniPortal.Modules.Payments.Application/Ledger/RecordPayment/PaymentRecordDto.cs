namespace NIMI.UniPortal.Modules.Payments.Application.Ledger.RecordPayment;

/// <summary>The recorded payment as the office sees it (P5.3).</summary>
public sealed record PaymentRecordDto(
    Guid Id,
    string ReceiptNumber,
    Guid StudentId,
    Guid FeeItemId,
    decimal Amount,
    string Method,
    string? Reference,
    DateTimeOffset PaidAt,
    string FeeStatus,
    decimal Balance);
