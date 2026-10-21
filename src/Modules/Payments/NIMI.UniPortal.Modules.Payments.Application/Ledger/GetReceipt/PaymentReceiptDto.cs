namespace NIMI.UniPortal.Modules.Payments.Application.Ledger.GetReceipt;

/// <summary>The printable receipt payload (P5.4); visible to the student and Student Affairs only.</summary>
public sealed record PaymentReceiptDto(
    Guid PaymentId,
    string ReceiptNumber,
    Guid StudentId,
    string StudentName,
    string? StudentCode,
    Guid FeeItemId,
    string FeeName,
    string FeeNameAr,
    decimal Amount,
    string Method,
    string? Reference,
    DateTimeOffset PaidAt);
