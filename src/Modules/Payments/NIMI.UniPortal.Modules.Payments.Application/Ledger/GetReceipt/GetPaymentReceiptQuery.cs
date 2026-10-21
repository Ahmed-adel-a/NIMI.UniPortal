using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Payments.Application.Ledger.GetReceipt;

/// <summary>P5.4 — one receipt, with an ownership check in the handler (others' → 404).</summary>
public sealed record GetPaymentReceiptQuery(
    Guid PaymentId,
    Guid CallerId,
    bool IsAdmin) : IQuery<PaymentReceiptDto>;
