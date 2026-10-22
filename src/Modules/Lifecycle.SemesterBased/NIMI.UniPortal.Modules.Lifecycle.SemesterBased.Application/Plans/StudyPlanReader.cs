using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Plans;
using NIMI.UniPortal.Modules.Scheduling.Contracts;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Plans;

/// <summary>Turns a saved plan into its DTO; codes and names come from Scheduling, never copied here.</summary>
internal static class StudyPlanReader
{
    public static async Task<StudyPlanDto> ToDtoAsync(StudyPlan plan, ISchedulingApi scheduling, CancellationToken cancellationToken)
    {
        var subjects = new List<StudyPlanSubjectDto>(plan.Subjects.Count);
        foreach (var entry in plan.Subjects)
        {
            var subject = await scheduling.GetSubjectAsync(entry.SubjectId, cancellationToken);
            subjects.Add(new StudyPlanSubjectDto(
                entry.Level, entry.Term, entry.SubjectId, subject?.Code ?? string.Empty, subject?.Name ?? string.Empty,
                subject?.NameAr ?? string.Empty, subject?.CreditHours ?? 0));
        }

        return new StudyPlanDto(
            plan.DepartmentId,
            plan.Levels,
            [.. subjects.OrderBy(s => s.Level).ThenBy(s => s.Term).ThenBy(s => s.Code, StringComparer.Ordinal)],
            plan.UpdatedAt);
    }
}
