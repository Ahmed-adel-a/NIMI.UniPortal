using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Payments.Application.Ledger.RecordPayment;

/// <summary>P5.3 — Student Affairs records an office payment (cash or bank transfer) against one fee.</summary>
public sealed record RecordPaymentCommand(
    Guid StudentId,
    Guid FeeItemId,
    decimal Amount,
    string Method,
    string? Reference,
    DateTimeOffset? PaidAt,
    Guid RecordedBy) : ICommand<PaymentRecordDto>;
