using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Grades.Application.Abstractions;
using NIMI.UniPortal.Modules.Grades.Domain;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Grades.Application.Mine;

/// <summary>
/// P9.3 — rule R1: published grades only. Term GPA per term; the cumulative GPA always spans every published term
/// and counts only the latest attempt of a retaken subject (P09 §6), even when one term is asked for.
/// </summary>
internal sealed class MyGradesHandler(IGradesDbContext db, ISchedulingApi scheduling)
    : IRequestHandler<MyGradesQuery, MyGradesDto>
{
    public async Task<MyGradesDto> Handle(MyGradesQuery query, CancellationToken cancellationToken)
    {
        if (!query.Caller.IsStudent)
            throw new ForbiddenException("Only students have grades.");

        var published = await db.FinalGrades.AsNoTracking()
            .Where(g => g.StudentId == query.Caller.UserId && g.Status == GradeStatus.Published)
            .ToListAsync(cancellationToken);

        var terms = new Dictionary<Guid, TermRef>();
        foreach (var termId in published.Select(g => g.TermId).Distinct())
        {
            if (await scheduling.GetTermAsync(termId, cancellationToken) is { } term)
                terms[termId] = term;
        }

        var subjects = new Dictionary<Guid, SubjectRef>();
        foreach (var subjectId in published.Select(g => g.SubjectId).Distinct())
        {
            if (await scheduling.GetSubjectAsync(subjectId, cancellationToken) is { } subject)
                subjects[subjectId] = subject;
        }

        var cumulative = Gpa.Cumulative(published
            .Where(g => terms.ContainsKey(g.TermId))
            .Select(g => new GpaAttempt(g.SubjectId, terms[g.TermId].StartsOn, g.CreditHours, g.GradePoints)));

        var termGroups = published
            .Where(g => terms.ContainsKey(g.TermId) && (query.TermId is null || g.TermId == query.TermId))
            .GroupBy(g => g.TermId)
            .OrderByDescending(group => terms[group.Key].StartsOn)
            .Select(group => new MyTermGradesDto(
                group.Key,
                terms[group.Key].AcademicYear,
                terms[group.Key].Number,
                Gpa.Calculate(group.Select(g => new GpaEntry(g.CreditHours, g.GradePoints))),
                [.. group
                    .Select(g => ToDto(g, subjects.GetValueOrDefault(g.SubjectId)))
                    .OrderBy(g => g.SubjectCode, StringComparer.Ordinal)]))
            .ToList();

        return new MyGradesDto(cumulative, termGroups);
    }

    private static MyGradeDto ToDto(FinalGrade grade, SubjectRef? subject) =>
        new(
            grade.Id,
            grade.SubjectId,
            subject?.Code ?? string.Empty,
            subject?.Name ?? string.Empty,
            subject?.NameAr ?? string.Empty,
            grade.CreditHours,
            grade.Score,
            grade.Grade,
            grade.GradePoints);
}
