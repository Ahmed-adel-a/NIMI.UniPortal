using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Plans;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Plans;

/// <summary>A plan subject with its catalog code, names and credit hours (read from Scheduling).</summary>
public sealed record StudyPlanSubjectDto(
    int Level, PlanTerm Term, Guid SubjectId, string Code, string Name, string NameAr, int CreditHours);
