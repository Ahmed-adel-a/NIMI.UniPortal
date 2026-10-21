namespace NIMI.UniPortal.Modules.Payments.Application.Ledger.GetPaymentReport;

/// <summary>One fee item's expected / collected row in the Student Affairs report (P5.6).</summary>
public sealed record PaymentReportRowDto(
    Guid FeeItemId,
    string Name,
    string Category,
    decimal Amount,
    decimal Expected,
    decimal Collected,
    int Payers,
    decimal Outstanding);
