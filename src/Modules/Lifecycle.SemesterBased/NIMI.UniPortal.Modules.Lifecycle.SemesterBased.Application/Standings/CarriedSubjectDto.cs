namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Standings;

/// <summary>A failed subject still to pass (مادة تخلف), with the plan level it belongs to.</summary>
public sealed record CarriedSubjectDto(Guid SubjectId, string Code, string Name, string NameAr, int Level);
