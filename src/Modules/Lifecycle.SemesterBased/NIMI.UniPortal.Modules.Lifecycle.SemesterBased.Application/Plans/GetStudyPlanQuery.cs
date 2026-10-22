using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Plans;

/// <summary>P4.7: a department's study plan (404 until Student Affairs saves one).</summary>
public sealed record GetStudyPlanQuery(Guid DepartmentId) : IQuery<StudyPlanDto>;
