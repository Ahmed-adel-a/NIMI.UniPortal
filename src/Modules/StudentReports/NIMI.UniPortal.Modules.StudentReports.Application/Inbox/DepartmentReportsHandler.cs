using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.StudentReports.Application.Abstractions;
using NIMI.UniPortal.Modules.StudentReports.Application.Common;
using NIMI.UniPortal.Modules.StudentReports.Domain;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.StudentReports.Application.Inbox;

internal sealed class DepartmentReportsHandler(IStudentReportsDbContext db, HeadAccess heads, ReportPresenter presenter)
    : IRequestHandler<DepartmentReportsQuery, PagedResult<ReportDto>>
{
    public async Task<PagedResult<ReportDto>> Handle(DepartmentReportsQuery query, CancellationToken cancellationToken)
    {
        var departmentId = await heads.DepartmentOfAsync(query.Caller, cancellationToken);
        var reports = db.Reports.AsNoTracking().Where(r => r.DepartmentId == departmentId);
        if (ReportNames.TryParse<ReportStatus>(query.Status, out var status))
            reports = reports.Where(r => r.Status == status);
        return await ReportPages.PageAsync(reports, query.Paging, presenter, cancellationToken);
    }
}
