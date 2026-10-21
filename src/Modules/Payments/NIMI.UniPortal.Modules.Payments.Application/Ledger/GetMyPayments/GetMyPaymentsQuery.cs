using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Payments.Application.Ledger.GetMyPayments;

/// <summary>P5.4 — the signed-in student's own payments, newest first.</summary>
public sealed record GetMyPaymentsQuery(Guid StudentId, PageRequest Paging)
    : IQuery<PagedResult<PaymentHistoryItemDto>>;
