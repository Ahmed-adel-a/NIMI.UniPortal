using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Plans;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Plans;

/// <summary>One line of a study plan as Student Affairs sends it.</summary>
public sealed record StudyPlanEntry(int Level, PlanTerm Term, Guid SubjectId);
