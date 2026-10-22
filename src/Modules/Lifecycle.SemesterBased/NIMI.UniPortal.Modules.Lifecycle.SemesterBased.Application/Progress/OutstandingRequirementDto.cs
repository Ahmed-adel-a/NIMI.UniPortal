namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Progress;

/// <summary>An active graduation-requirement subject the student has not passed yet (P4.6, decision MILEDU).</summary>
public sealed record OutstandingRequirementDto(Guid SubjectId, string Code, string Name, string NameAr);
