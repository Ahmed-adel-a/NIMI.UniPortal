using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Grades.Contracts;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Abstractions;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Promotion;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Enrollment;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Promotion;
using NIMI.UniPortal.Modules.Scheduling.Contracts;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.IntegrationEvents;

/// <summary>
/// Published (or corrected) grades pass or fail the matching term enrollments. After the second term of the year,
/// every affected student whose results are now complete is decided automatically (decision SEMPROMO), once per
/// academic year; a later correction does not re-decide: Student Affairs overrides instead.
/// </summary>
internal sealed class GradesPublishedHandler(
    ISemesterDbContext db, ISchedulingApi scheduling, PromotionService promotions, IEventPublisher events, TimeProvider clock)
    : IIntegrationEventHandler<GradesPublished>
{
    public async Task HandleAsync(GradesPublished integrationEvent, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var studentIds = integrationEvent.Grades.Select(g => g.StudentId).Distinct().ToList();
        var enrollments = await db.TermEnrollments
            .Where(e => e.TermId == integrationEvent.TermId &&
                        studentIds.Contains(e.StudentId) &&
                        e.Status != EnrollmentStatus.Withdrawn)
            .ToListAsync(cancellationToken);

        foreach (var grade in integrationEvent.Grades)
        {
            foreach (var enrollment in enrollments.Where(e => e.StudentId == grade.StudentId && e.SubjectId == grade.SubjectId))
                enrollment.RecordResult(grade.Passed, grade.GradePoints, grade.CreditHours, now);
        }

        await db.SaveChangesAsync(cancellationToken);   // event handlers save themselves; results first

        var term = await scheduling.GetTermAsync(integrationEvent.TermId, cancellationToken);
        if (term?.Number != "second")
            return;

        var decisions = new List<YearPromotion>();
        foreach (var standing in await promotions.ReadyForDecisionAsync(term.Id, term.AcademicYear, studentIds, cancellationToken))
        {
            if (await promotions.DecideAsync(standing, term.AcademicYear, cancellationToken) is { } decision)
                decisions.Add(decision);
        }

        if (decisions.Count == 0)
            return;

        await db.SaveChangesAsync(cancellationToken);
        foreach (var decision in decisions)
            await events.PublishAsync(PromotionNotices.For(decision), cancellationToken);
    }
}
