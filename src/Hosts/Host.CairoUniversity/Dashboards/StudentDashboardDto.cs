namespace Host.CairoUniversity.Dashboards;

/// <summary>
/// P10.1 — the student's home screen in one call. A section whose source failed is null and its name is listed in
/// <see cref="Unavailable"/>; otherwise empty means empty (no lectures, nothing due, no closed session yet …).
/// </summary>
public sealed record StudentDashboardDto(
    IReadOnlyList<DashboardLectureDto>? Today,
    DashboardLectureDto? NextLecture,
    IReadOnlyList<DashboardAssignmentDto>? UpcomingAssignments,
    IReadOnlyList<DashboardAnnouncementDto>? RecentAnnouncements,
    decimal? AttendanceRate,
    int? UnreadNotifications,
    decimal? OutstandingFees,
    IReadOnlyList<string> Unavailable,
    DateTimeOffset GeneratedAt);
