namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Standings;

/// <summary>One year group (فرقة) of a department: how many students stand in it, by status.</summary>
public sealed record CohortLevelDto(int Level, int Active, int Repeating, int External, int OnHold, int Total);
