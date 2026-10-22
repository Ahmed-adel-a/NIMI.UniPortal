namespace NIMI.UniPortal.Hosting.Dashboards;

/// <summary>Today's lectures and the next one that hasn't ended (one read, two dashboard sections).</summary>
internal sealed record DashboardLectures(IReadOnlyList<DashboardLectureDto> Today, DashboardLectureDto? Next);
