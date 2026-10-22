namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Rosters;

/// <summary>A confirmed member of a group as the roster shows them (P4.4); the Users contract has no email.</summary>
public sealed record RosterStudentDto(Guid StudentId, string FullName, string StudentCode);
