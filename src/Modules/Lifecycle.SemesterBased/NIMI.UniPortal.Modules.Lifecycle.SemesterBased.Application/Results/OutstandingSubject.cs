namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Results;

/// <summary>A failed subject not passed since, with the plan level it belongs to.</summary>
internal sealed record OutstandingSubject(Guid SubjectId, int Level);
