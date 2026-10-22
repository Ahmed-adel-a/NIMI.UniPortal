using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Plans;

/// <summary>P4.7: Student Affairs saves a department's whole study plan (PUT semantics).</summary>
public sealed record SaveStudyPlanCommand(Guid DepartmentId, int Levels, IReadOnlyList<StudyPlanEntry>? Subjects)
    : ICommand<StudyPlanDto>;
