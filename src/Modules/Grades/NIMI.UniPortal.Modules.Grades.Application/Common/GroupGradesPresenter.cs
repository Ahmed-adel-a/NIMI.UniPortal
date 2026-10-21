using NIMI.UniPortal.Modules.Grades.Domain;
using NIMI.UniPortal.Modules.Users.Contracts;

namespace NIMI.UniPortal.Modules.Grades.Application.Common;

/// <summary>Builds the roster × grades view; names and codes come from Users, never copied into this schema.</summary>
internal sealed class GroupGradesPresenter(IUsersApi users)
{
    public async Task<GroupGradesDto> PresentAsync(
        GradedGroup graded,
        IReadOnlyList<Guid> roster,
        IReadOnlyDictionary<Guid, FinalGrade> gradesByStudent,
        CancellationToken cancellationToken)
    {
        var rows = new List<GroupGradeRowDto>(roster.Count);
        foreach (var studentId in roster)
        {
            var user = await users.GetUserAsync(studentId, cancellationToken);
            var grade = gradesByStudent.GetValueOrDefault(studentId);
            rows.Add(new GroupGradeRowDto(
                studentId,
                user?.FullName ?? string.Empty,
                user?.StudentCode,
                grade?.Id,
                grade?.Score,
                grade?.Grade,
                grade?.GradePoints,
                grade?.Status));
        }

        var (group, subject) = (graded.Group, graded.Subject);
        return new GroupGradesDto(
            group.Id,
            group.GroupNumber,
            group.TermId,
            subject.Id,
            subject.Code,
            subject.Name,
            subject.NameAr,
            subject.CreditHours,
            [.. rows.OrderBy(r => r.StudentCode, StringComparer.Ordinal).ThenBy(r => r.StudentId)]);
    }
}
