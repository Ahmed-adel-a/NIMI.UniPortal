using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.StudentReports.Application.Abstractions;
using NIMI.UniPortal.Modules.StudentReports.Application.Common;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.StudentReports.Application.Mine;

internal sealed class MyReportsHandler(IStudentReportsDbContext db, ReportPresenter presenter)
    : IRequestHandler<MyReportsQuery, PagedResult<StudentReportDto>>
{
    public async Task<PagedResult<StudentReportDto>> Handle(MyReportsQuery query, CancellationToken cancellationToken)
    {
        if (!query.Caller.IsStudent)
            throw new ForbiddenException("Only students have reports of their own.");
        var reports = db.Reports.AsNoTracking().Where(r => r.StudentId == query.Caller.UserId);
        return await ReportPages.PageAsync(reports, query.Paging, presenter, cancellationToken);
    }
}
