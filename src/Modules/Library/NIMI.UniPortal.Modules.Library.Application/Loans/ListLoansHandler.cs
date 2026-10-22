using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Library.Application.Abstractions;
using NIMI.UniPortal.Modules.Library.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Library.Application.Loans;

internal sealed class ListLoansHandler(ILibraryDbContext db, LoanPresenter presenter, TimeProvider clock)
    : IRequestHandler<ListLoansQuery, PagedResult<LoanDto>>
{
    public async Task<PagedResult<LoanDto>> Handle(ListLoansQuery query, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var loans = db.Loans.AsNoTracking();
        if (query.Overdue)
            loans = loans.Where(l => l.ReturnedAt == null && l.DueAt < now);
        if (query.Open is { } open)
            loans = open ? loans.Where(l => l.ReturnedAt == null) : loans.Where(l => l.ReturnedAt != null);
        if (query.UserId is { } userId)
            loans = loans.Where(l => l.UserId == userId);

        var total = await loans.CountAsync(cancellationToken);
        var page = await loans
            .OrderBy(l => l.DueAt)
            .ThenBy(l => l.Id)
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<LoanDto>(await presenter.PresentAllAsync(page, cancellationToken), query.Paging.Page, query.Paging.PageSize, total);
    }
}
