using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Payments.Application.Fees.ListFeeItems;

/// <summary>P5.1 — the fee-item catalog, optionally filtered by term, category and active state.</summary>
public sealed record ListFeeItemsQuery(
    Guid? TermId,
    string? Category,
    bool IncludeInactive,
    PageRequest Paging) : IQuery<PagedResult<FeeItemDto>>;
