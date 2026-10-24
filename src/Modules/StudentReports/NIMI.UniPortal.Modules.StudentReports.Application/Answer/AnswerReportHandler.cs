using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Audit.Contracts;
using NIMI.UniPortal.Modules.Notifications.Contracts;
using NIMI.UniPortal.Modules.StudentReports.Application.Abstractions;
using NIMI.UniPortal.Modules.StudentReports.Application.Common;
using NIMI.UniPortal.Modules.StudentReports.Domain;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.StudentReports.Application.Answer;

/// <summary>
/// Decision ROLES2 — only the head of the report's department answers; a report outside their department is a 404.
/// One reply per report; a closed report takes nothing more (409). Every answer and close is audited.
/// </summary>
internal sealed class AnswerReportHandler(
    IStudentReportsDbContext db,
    HeadAccess heads,
    ReportPresenter presenter,
    IEventCollector events,
    TimeProvider clock) : IRequestHandler<AnswerReportCommand, StudentReportDto>
{
    public async Task<StudentReportDto> Handle(AnswerReportCommand command, CancellationToken cancellationToken)
    {
        var departmentId = await heads.DepartmentOfAsync(command.Caller, cancellationToken);
        var report = await db.Reports.SingleOrDefaultAsync(
                r => r.Id == command.ReportId && r.DepartmentId == departmentId, cancellationToken)
            ?? throw new NotFoundException("Report not found.");

        var now = clock.GetUtcNow();
        if (command.Reply is { } reply)
        {
            if (!report.CanAnswer)
                throw new ConflictException(report.Status == ReportStatus.Closed ? "The report is closed." : "The report was already answered.");
            report.Answer(command.Caller.UserId, reply, now);
            Audit(AuditActions.StudentReportAnswered, report, $"Report \"{report.Title}\" answered.", command.Caller.UserId, now);
        }

        if (command.Close)
        {
            if (!report.CanClose)
                throw new ConflictException("The report is already closed.");
            report.Close(command.Caller.UserId, now);
            Audit(AuditActions.StudentReportClosed, report, $"Report \"{report.Title}\" closed.", command.Caller.UserId, now);
        }

        var answered = command.Reply is not null;
        events.Add(new NotificationRequested(
            Guid.CreateVersion7(),
            now,
            [report.StudentId],
            "system",
            answered ? "Your report was answered" : "Your report was closed",
            answered ? "تم الرد على بلاغك" : "تم إغلاق بلاغك",
            report.Title.Length <= 200 ? report.Title : report.Title[..197] + "...",
            report.Title.Length <= 200 ? report.Title : report.Title[..197] + "...",
            $"/reports/{report.Id}"));

        return await presenter.PresentAsync(report, cancellationToken);
    }

    private void Audit(string action, StudentReport report, string summary, Guid actorId, DateTimeOffset now) =>
        events.Add(new AuditableActionPerformed(
            Guid.CreateVersion7(), now, actorId, action, AuditEntities.StudentReport, report.Id, summary));
}
