using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Forum.Application.Abstractions;
using NIMI.UniPortal.Modules.Forum.Application.Common;
using NIMI.UniPortal.Modules.Forum.Domain;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Forum.Application.Reports;

internal sealed class ListReportsHandler(IForumDbContext db) : IRequestHandler<ListReportsQuery, PagedResult<ReportDto>>
{
    public async Task<PagedResult<ReportDto>> Handle(ListReportsQuery query, CancellationToken cancellationToken)
    {
        var reports = db.Reports.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<ForumReportStatus>(query.Status, ignoreCase: true, out var status) || !Enum.IsDefined(status))
                throw ValidationErrors.For("status", "Status must be open or resolved.");
            reports = reports.Where(r => r.Status == status);
        }

        var total = await reports.CountAsync(cancellationToken);
        var page = await reports
            .OrderBy(r => r.CreatedAt)
            .ThenBy(r => r.Id)
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ReportDto>([.. page.Select(ReportDto.From)], query.Paging.Page, query.Paging.PageSize, total);
    }
}
