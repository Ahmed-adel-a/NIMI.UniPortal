using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Grades.Application.Abstractions;
using NIMI.UniPortal.Modules.Grades.Contracts;
using NIMI.UniPortal.Modules.Grades.Domain;
using NIMI.UniPortal.Modules.Notifications.Contracts;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Grades.Application.Publishing;

/// <summary>
/// P9.2 — confirmed → published (drafts are never published). After the commit the lifecycle hears
/// <see cref="GradesPublished"/> and every affected student gets one notification.
/// </summary>
internal sealed class PublishGradesHandler(
    IGradesDbContext db, ISchedulingApi scheduling, IEventCollector events, TimeProvider clock)
    : IRequestHandler<PublishGradesCommand, PublishResultDto>
{
    public async Task<PublishResultDto> Handle(PublishGradesCommand command, CancellationToken cancellationToken)
    {
        if (!command.Caller.IsAdmin)
            throw new ForbiddenException("Only Student Affairs publishes grades.");

        var termId = command.TermId!.Value;
        _ = await scheduling.GetTermAsync(termId, cancellationToken)
            ?? throw new NotFoundException("Term not found.");

        var query = db.FinalGrades.Where(g => g.TermId == termId && g.Status == GradeStatus.Confirmed);
        if (command.SubjectId is { } subjectId)
            query = query.Where(g => g.SubjectId == subjectId);
        if (command.GroupId is { } groupId)
            query = query.Where(g => g.SubjectGroupId == groupId);
        var confirmed = await query.ToListAsync(cancellationToken);
        if (confirmed.Count == 0)
            return new PublishResultDto(0);

        var now = clock.GetUtcNow();
        foreach (var grade in confirmed)
            grade.Publish(command.Caller.UserId, now);

        events.Add(new GradesPublished(
            Guid.CreateVersion7(),
            now,
            termId,
            [.. confirmed.Select(g => new PublishedGrade(g.StudentId, g.SubjectId, g.CreditHours, g.GradePoints, g.Passed))]));
        events.Add(new NotificationRequested(
            Guid.CreateVersion7(),
            now,
            [.. confirmed.Select(g => g.StudentId).Distinct()],
            "grade",
            "Grades published",
            "تم نشر الدرجات",
            "New grades are available in your results.",
            "تم نشر درجات جديدة في نتائجك.",
            "/grades"));

        return new PublishResultDto(confirmed.Count);
    }
}
