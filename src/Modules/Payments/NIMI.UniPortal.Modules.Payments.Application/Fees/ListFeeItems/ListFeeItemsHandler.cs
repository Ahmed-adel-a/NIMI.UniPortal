using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Payments.Application.Abstractions;
using NIMI.UniPortal.Modules.Payments.Application.Common;
using NIMI.UniPortal.Modules.Payments.Domain.Fees;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Payments.Application.Fees.ListFeeItems;

internal sealed class ListFeeItemsHandler(IPaymentsDbContext db)
    : IRequestHandler<ListFeeItemsQuery, PagedResult<FeeItemDto>>
{
    public async Task<PagedResult<FeeItemDto>> Handle(
        ListFeeItemsQuery query, CancellationToken cancellationToken)
    {
        FeeCategory? category = null;
        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            if (!FeeCategoryNames.TryParse(query.Category, out var parsed))
                throw ValidationErrors.For(nameof(query.Category), "Unknown fee category.");

            category = parsed;
        }

        var items = db.FeeItems.AsNoTracking()
            .Where(f => (query.TermId == null || f.TermId == null || f.TermId == query.TermId)
                        && (query.IncludeInactive || f.IsActive));

        if (category is { } value)
            items = items.Where(f => f.Category == value);

        var total = await items.CountAsync(cancellationToken);
        var rows = await items
            .OrderBy(f => f.Name)
            .ThenBy(f => f.Id)
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<FeeItemDto>(
            rows.Select(FeeItemDto.From).ToList(),
            query.Paging.Page,
            query.Paging.PageSize,
            total);
    }
}
