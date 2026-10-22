namespace NIMI.UniPortal.Hosting.Lifecycle;

/// <summary>An active graduation-requirement subject the student has not passed yet (decision MILEDU).</summary>
public sealed record OutstandingRequirementDto(Guid SubjectId, string Code, string Name, string NameAr);
