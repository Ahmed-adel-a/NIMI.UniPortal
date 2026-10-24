using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.StudentReports.Application.Abstractions;
using NIMI.UniPortal.Modules.StudentReports.Application.Common;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.StudentReports.Application.Detail;

internal sealed class GetReportHandler(IStudentReportsDbContext db, HeadAccess heads, ReportPresenter presenter)
    : IRequestHandler<GetReportQuery, ReportDto>
{
    public async Task<ReportDto> Handle(GetReportQuery query, CancellationToken cancellationToken)
    {
        var report = await db.Reports.AsNoTracking().SingleOrDefaultAsync(r => r.Id == query.ReportId, cancellationToken);
        var allowed = report is not null &&
                      (query.Caller.IsAdmin ||
                       (query.Caller.IsStudent && report.StudentId == query.Caller.UserId) ||
                       await heads.DepartmentOrNullAsync(query.Caller, cancellationToken) == report.DepartmentId);
        return allowed
            ? await presenter.PresentAsync(report!, cancellationToken)
            : throw new NotFoundException("Report not found.");
    }
}
