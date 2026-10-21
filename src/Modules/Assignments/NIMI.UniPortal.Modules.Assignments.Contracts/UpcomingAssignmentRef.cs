namespace NIMI.UniPortal.Modules.Assignments.Contracts;

/// <summary>An open assignment still waiting for the student's submission.</summary>
public sealed record UpcomingAssignmentRef(Guid Id, Guid SubjectId, string SubjectCode, string Title, DateTimeOffset DueAt);
