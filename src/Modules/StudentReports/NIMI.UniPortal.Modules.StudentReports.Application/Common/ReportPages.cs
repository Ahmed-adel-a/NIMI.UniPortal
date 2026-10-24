using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.StudentReports.Domain;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.StudentReports.Application.Common;

/// <summary>One page of reports, newest first.</summary>
internal static class ReportPages
{
    public static async Task<PagedResult<ReportDto>> PageAsync(
        IQueryable<StudentReport> reports, PageRequest paging, ReportPresenter presenter, CancellationToken cancellationToken)
    {
        var total = await reports.CountAsync(cancellationToken);
        var page = await reports
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<ReportDto>(await presenter.PresentAllAsync(page, cancellationToken), paging.Page, paging.PageSize, total);
    }
}
