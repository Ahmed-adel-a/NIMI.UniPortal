using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Payments.Application.Abstractions;
using NIMI.UniPortal.Modules.Payments.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Payments.Application.Ledger.GetMyPayments;

internal sealed class GetMyPaymentsHandler(IPaymentsDbContext db)
    : IRequestHandler<GetMyPaymentsQuery, PagedResult<PaymentHistoryItemDto>>
{
    public async Task<PagedResult<PaymentHistoryItemDto>> Handle(
        GetMyPaymentsQuery query, CancellationToken cancellationToken)
    {
        var rows = from p in db.Payments.AsNoTracking()
                   join sf in db.StudentFees.AsNoTracking() on p.StudentFeeId equals sf.Id
                   join f in db.FeeItems.AsNoTracking() on sf.FeeItemId equals f.Id
                   where p.StudentId == query.StudentId
                   select new
                   {
                       p.Id,
                       p.ReceiptNumber,
                       FeeItemId = f.Id,
                       f.Name,
                       f.NameAr,
                       p.Amount,
                       p.Method,
                       p.PaidAt,
                   };

        var total = await rows.CountAsync(cancellationToken);
        var data = await rows
            .OrderByDescending(x => x.PaidAt)
            .ThenByDescending(x => x.Id)
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .ToListAsync(cancellationToken);

        var items = data
            .Select(x => new PaymentHistoryItemDto(
                x.Id,
                x.ReceiptNumber,
                x.FeeItemId,
                x.Name,
                x.NameAr,
                x.Amount,
                PaymentMethodNames.ToWire(x.Method),
                x.PaidAt))
            .ToList();

        return new PagedResult<PaymentHistoryItemDto>(
            items, query.Paging.Page, query.Paging.PageSize, total);
    }
}
