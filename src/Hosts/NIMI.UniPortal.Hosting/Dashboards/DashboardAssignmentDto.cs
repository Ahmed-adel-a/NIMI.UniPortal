namespace NIMI.UniPortal.Hosting.Dashboards;

/// <summary>An open assignment due within 7 days that the student has not submitted.</summary>
public sealed record DashboardAssignmentDto(Guid Id, Guid SubjectId, string SubjectCode, string Title, DateTimeOffset DueAt);
