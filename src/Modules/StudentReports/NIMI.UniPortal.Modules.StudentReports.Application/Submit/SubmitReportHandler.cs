using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Notifications.Contracts;
using NIMI.UniPortal.Modules.StudentReports.Application.Abstractions;
using NIMI.UniPortal.Modules.StudentReports.Application.Common;
using NIMI.UniPortal.Modules.StudentReports.Domain;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.StudentReports.Application.Submit;

/// <summary>
/// Decision ROLES2 — the report goes to the student's department; its active head (if any) is notified after the
/// commit. A department without a head still takes reports: Student Affairs sees them, and so will the next head.
/// </summary>
internal sealed class SubmitReportHandler(
    IStudentReportsDbContext db,
    IUsersApi users,
    ReportPresenter presenter,
    IEventCollector events,
    TimeProvider clock) : IRequestHandler<SubmitReportCommand, ReportDto>
{
    public async Task<ReportDto> Handle(SubmitReportCommand command, CancellationToken cancellationToken)
    {
        if (!command.Caller.IsStudent)
            throw new ForbiddenException("Only students send reports to their department head.");
        var placement = await users.GetStudentPlacementAsync(command.Caller.UserId, cancellationToken)
            ?? throw new ForbiddenException("Your account has no department.");

        ReportNames.TryParse<ReportCategory>(command.Category, out var category);
        var now = clock.GetUtcNow();
        var report = StudentReport.Submit(command.Caller.UserId, placement.DepartmentId, category, command.Title, command.Message, now);
        db.Reports.Add(report);

        if (await users.GetDepartmentHeadAsync(placement.DepartmentId, cancellationToken) is { } head)
        {
            events.Add(new NotificationRequested(
                Guid.CreateVersion7(),
                now,
                [head.Id],
                "system",
                "New report from a student",
                "بلاغ جديد من طالب",
                Clip(report.Title),
                Clip(report.Title),
                $"/reports/{report.Id}"));
        }

        return await presenter.PresentAsync(report, cancellationToken);
    }

    private static string Clip(string text) => text.Length <= 200 ? text : text[..197] + "...";
}
