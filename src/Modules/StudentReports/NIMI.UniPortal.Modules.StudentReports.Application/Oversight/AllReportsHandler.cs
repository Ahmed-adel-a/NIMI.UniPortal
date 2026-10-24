using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.StudentReports.Application.Abstractions;
using NIMI.UniPortal.Modules.StudentReports.Application.Common;
using NIMI.UniPortal.Modules.StudentReports.Domain;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.StudentReports.Application.Oversight;

/// <summary>Read-only oversight; the route is restricted to Student Affairs by the Admin policy.</summary>
internal sealed class AllReportsHandler(IStudentReportsDbContext db, ReportPresenter presenter)
    : IRequestHandler<AllReportsQuery, PagedResult<StudentReportDto>>
{
    public async Task<PagedResult<StudentReportDto>> Handle(AllReportsQuery query, CancellationToken cancellationToken)
    {
        var reports = db.Reports.AsNoTracking();
        if (query.DepartmentId is { } departmentId)
            reports = reports.Where(r => r.DepartmentId == departmentId);
        if (ReportNames.TryParse<ReportStatus>(query.Status, out var status))
            reports = reports.Where(r => r.Status == status);
        return await ReportPages.PageAsync(reports, query.Paging, presenter, cancellationToken);
    }
}
