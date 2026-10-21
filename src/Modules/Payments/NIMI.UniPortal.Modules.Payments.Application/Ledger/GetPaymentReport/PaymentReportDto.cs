namespace NIMI.UniPortal.Modules.Payments.Application.Ledger.GetPaymentReport;

/// <summary>The whole tracking report (P5.6).</summary>
public sealed record PaymentReportDto(IReadOnlyList<PaymentReportRowDto> Items);
