using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Library.Application.Abstractions;
using NIMI.UniPortal.Modules.Library.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Library.Application.Loans;

internal sealed class MyLoansHandler(ILibraryDbContext db, LoanPresenter presenter) : IRequestHandler<MyLoansQuery, PagedResult<LoanDto>>
{
    public async Task<PagedResult<LoanDto>> Handle(MyLoansQuery query, CancellationToken cancellationToken)
    {
        var loans = db.Loans.AsNoTracking().Where(l => l.UserId == query.Caller.UserId);

        var total = await loans.CountAsync(cancellationToken);
        var page = await loans
            .OrderBy(l => l.ReturnedAt != null)
            .ThenBy(l => l.DueAt)
            .ThenBy(l => l.Id)
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<LoanDto>(await presenter.PresentAllAsync(page, cancellationToken), query.Paging.Page, query.Paging.PageSize, total);
    }
}
