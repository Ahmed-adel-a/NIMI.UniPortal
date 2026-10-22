namespace NIMI.UniPortal.Hosting.Lifecycle;

/// <summary>A member of a group as the roster shows them (P4.4), whichever academic system they study in.</summary>
public sealed record RosterStudentDto(Guid StudentId, string FullName, string StudentCode);
