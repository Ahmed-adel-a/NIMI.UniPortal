using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Abstractions;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.RegistrationWindows.ListRegistrationWindows;

internal sealed class ListRegistrationWindowsHandler(ICreditHoursDbContext db, TimeProvider clock)
    : IRequestHandler<ListRegistrationWindowsQuery, PagedResult<RegistrationWindowDto>>
{
    public async Task<PagedResult<RegistrationWindowDto>> Handle(
        ListRegistrationWindowsQuery query, CancellationToken cancellationToken)
    {
        var windows = db.RegistrationWindows.AsNoTracking();

        var total = await windows.CountAsync(cancellationToken);
        var page = await windows
            .OrderByDescending(w => w.OpensAt)
            .ThenByDescending(w => w.Id)
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .ToListAsync(cancellationToken);

        var now = clock.GetUtcNow();
        return new PagedResult<RegistrationWindowDto>(
            page.Select(w => RegistrationWindowDto.From(w, now)).ToList(),
            query.Paging.Page,
            query.Paging.PageSize,
            total);
    }
}
