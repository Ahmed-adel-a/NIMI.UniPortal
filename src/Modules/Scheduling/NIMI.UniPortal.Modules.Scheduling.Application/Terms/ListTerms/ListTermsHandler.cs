using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Terms.ListTerms;

internal sealed class ListTermsHandler(ISchedulingDbContext db)
    : IRequestHandler<ListTermsQuery, PagedResult<TermDto>>
{
    public async Task<PagedResult<TermDto>> Handle(ListTermsQuery query, CancellationToken cancellationToken)
    {
        var terms = db.Terms.AsNoTracking();

        var total = await terms.CountAsync(cancellationToken);
        var items = await terms
            .OrderByDescending(t => t.StartsOn)
            .ThenByDescending(t => t.Id)
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .Select(t => new TermDto(t.Id, t.AcademicYear, t.Number, t.StartsOn, t.EndsOn, t.WeekCount, t.Status))
            .ToListAsync(cancellationToken);

        return new PagedResult<TermDto>(items, query.Paging.Page, query.Paging.PageSize, total);
    }
}
