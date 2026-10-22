using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Grades.Contracts;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Abstractions;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Common;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Results;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.IntegrationEvents;

/// <summary>
/// P9.3b — published (or corrected) grades become ledger rows (one per student, term and subject — a replay or a
/// correction updates it), complete or fail the matching confirmed registration, and refresh the students' GPA
/// snapshots. Pending and dropped registrations are left alone.
/// </summary>
internal sealed class GradesPublishedHandler(
    ICreditHoursDbContext db, StudentResults results, CreditHoursStudents creditHoursStudents, TimeProvider clock)
    : IIntegrationEventHandler<GradesPublished>
{
    public async Task HandleAsync(GradesPublished integrationEvent, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var termId = integrationEvent.TermId;

        // Decision LIFECYCLEDEPT: grades of year-group students belong to the other variant.
        var studentIds = new List<Guid>();
        foreach (var studentId in integrationEvent.Grades.Select(g => g.StudentId).Distinct())
        {
            if (await creditHoursStudents.IsOnCreditHoursAsync(studentId, cancellationToken))
                studentIds.Add(studentId);
        }

        if (studentIds.Count == 0)
            return;

        var ledger = await db.CreditLedger
            .Where(e => e.TermId == termId && studentIds.Contains(e.StudentId))
            .ToDictionaryAsync(e => (e.StudentId, e.SubjectId), cancellationToken);
        var registrations = await db.CourseRegistrations
            .Where(r => r.TermId == termId &&
                        studentIds.Contains(r.StudentId) &&
                        (r.Status == RegistrationStatus.Registered ||
                         r.Status == RegistrationStatus.Completed ||
                         r.Status == RegistrationStatus.Failed))
            .ToListAsync(cancellationToken);

        foreach (var grade in integrationEvent.Grades.Where(g => studentIds.Contains(g.StudentId)))
        {
            var key = (grade.StudentId, grade.SubjectId);
            if (ledger.TryGetValue(key, out var entry))
            {
                entry.Update(grade.CreditHours, grade.GradePoints, grade.Passed, now);
            }
            else
            {
                entry = CreditLedgerEntry.Record(
                    grade.StudentId, termId, grade.SubjectId, grade.CreditHours, grade.GradePoints, grade.Passed, now);
                db.CreditLedger.Add(entry);
                ledger[key] = entry;
            }

            foreach (var registration in registrations.Where(r => r.StudentId == grade.StudentId && r.SubjectId == grade.SubjectId))
                registration.RecordResult(grade.Passed, now);
        }

        // Snapshots are rebuilt from the saved ledger, so the ledger is saved first; event handlers save themselves.
        await db.SaveChangesAsync(cancellationToken);
        foreach (var studentId in studentIds)
            await results.RecomputeAsync(studentId, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }
}
