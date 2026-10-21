using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Payments.Application.Abstractions;
using NIMI.UniPortal.Modules.Payments.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Payments.Application.Ledger.ListPayments;

internal sealed class ListPaymentsHandler(IPaymentsDbContext db)
    : IRequestHandler<ListPaymentsQuery, PagedResult<AdminPaymentDto>>
{
    public async Task<PagedResult<AdminPaymentDto>> Handle(
        ListPaymentsQuery query, CancellationToken cancellationToken)
    {
        var rows = from p in db.Payments.AsNoTracking()
                   join sf in db.StudentFees.AsNoTracking() on p.StudentFeeId equals sf.Id
                   join f in db.FeeItems.AsNoTracking() on sf.FeeItemId equals f.Id
                   select new
                   {
                       p.Id,
                       p.ReceiptNumber,
                       p.StudentId,
                       FeeItemId = f.Id,
                       p.Amount,
                       p.Method,
                       p.PaidAt,
                   };

        if (query.StudentId is { } studentId)
            rows = rows.Where(x => x.StudentId == studentId);

        if (query.FeeItemId is { } feeItemId)
            rows = rows.Where(x => x.FeeItemId == feeItemId);

        if (query.From is { } from)
        {
            var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            rows = rows.Where(x => x.PaidAt >= fromUtc);
        }

        if (query.To is { } to)
        {
            var toUtc = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            rows = rows.Where(x => x.PaidAt < toUtc);
        }

        var total = await rows.CountAsync(cancellationToken);
        var data = await rows
            .OrderByDescending(x => x.PaidAt)
            .ThenByDescending(x => x.Id)
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .ToListAsync(cancellationToken);

        var items = data
            .Select(x => new AdminPaymentDto(
                x.Id,
                x.ReceiptNumber,
                x.StudentId,
                x.FeeItemId,
                x.Amount,
                PaymentMethodNames.ToWire(x.Method),
                x.PaidAt))
            .ToList();

        return new PagedResult<AdminPaymentDto>(
            items, query.Paging.Page, query.Paging.PageSize, total);
    }
}
