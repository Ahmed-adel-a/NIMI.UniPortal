using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Grades.Application.Abstractions;
using NIMI.UniPortal.Modules.Grades.Application.Common;
using NIMI.UniPortal.Modules.Grades.Contracts;
using NIMI.UniPortal.Modules.Grades.Domain;
using NIMI.UniPortal.Modules.Notifications.Contracts;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Grades.Application.Corrections;

/// <summary>
/// Rule R4 — only published grades are corrected, each with an audit row. The letter is re-read from the subject
/// faculty's current scale; the corrected grade is re-published for the lifecycle and the student is notified.
/// </summary>
internal sealed class CorrectGradeHandler(
    IGradesDbContext db, ISchedulingApi scheduling, GradeScales scales, IEventCollector events, TimeProvider clock)
    : IRequestHandler<CorrectGradeCommand, FinalGradeDto>
{
    public async Task<FinalGradeDto> Handle(CorrectGradeCommand command, CancellationToken cancellationToken)
    {
        if (!command.Caller.IsAdmin)
            throw new ForbiddenException("Only Student Affairs corrects grades.");

        var grade = await db.FinalGrades.SingleOrDefaultAsync(g => g.Id == command.GradeId, cancellationToken)
            ?? throw new NotFoundException("Grade not found.");
        if (grade.Status != GradeStatus.Published)
            throw new ConflictException("Only a published grade can be corrected; drafts and confirmed grades go through the teacher.");

        var subject = await scheduling.GetSubjectAsync(grade.SubjectId, cancellationToken)
            ?? throw new NotFoundException("Subject not found.");
        var scale = await scales.ForSubjectAsync(subject, cancellationToken);
        var now = clock.GetUtcNow();
        var correction = grade.Correct(command.Score, scale.Resolve(command.Score), command.Reason!, command.Caller.UserId, now);
        db.GradeCorrections.Add(correction);

        events.Add(new GradesPublished(
            Guid.CreateVersion7(),
            now,
            grade.TermId,
            [new PublishedGrade(grade.StudentId, grade.SubjectId, grade.CreditHours, grade.GradePoints, grade.Passed)]));
        events.Add(new NotificationRequested(
            Guid.CreateVersion7(),
            now,
            [grade.StudentId],
            "grade",
            $"Grade corrected: {subject.Code}",
            $"تم تعديل درجة: {subject.Code}",
            $"Your grade in {subject.Name} is now {grade.Grade}.",
            $"درجتك في {subject.NameAr} أصبحت {grade.Grade}.",
            "/grades"));

        return FinalGradeDto.From(grade);
    }
}
