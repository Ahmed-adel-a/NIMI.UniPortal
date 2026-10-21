namespace Host.CairoUniversity.Dashboards;

/// <summary>One of the latest announcements of the student's subjects.</summary>
public sealed record DashboardAnnouncementDto(
    Guid Id, Guid SubjectId, string SubjectCode, string Title, string Priority, bool IsPinned, DateTimeOffset CreatedAt);
