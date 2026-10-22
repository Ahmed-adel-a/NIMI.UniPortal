using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.Modules.Announcements.Contracts;
using NIMI.UniPortal.Modules.Assignments.Contracts;
using NIMI.UniPortal.Modules.Attendance.Contracts;
using NIMI.UniPortal.Modules.Notifications.Contracts;
using NIMI.UniPortal.Modules.Payments.Contracts;
using NIMI.UniPortal.Modules.Platform.Contracts;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;

namespace NIMI.UniPortal.Hosting.Dashboards;

/// <summary>
/// P10.1 — composes the student dashboard from module contracts only (host-level, no module of its own). Each
/// section is read on its own: a failing source is logged and reported in <c>unavailable</c>, the rest still returns.
/// Sections run one after another because they share the request's scoped DbContexts.
/// </summary>
internal sealed class StudentDashboardComposer(
    ISchedulingApi scheduling,
    IAcademicLifecycle lifecycle,
    IUsersApi users,
    IPlatformSettingsApi platform,
    IAssignmentsApi assignments,
    IAnnouncementsApi announcements,
    IAttendanceApi attendance,
    INotificationsApi notifications,
    IPaymentsApi payments,
    TimeProvider clock,
    ILogger<StudentDashboardComposer> logger)
{
    public const int AnnouncementCount = 5;
    public static readonly TimeSpan AssignmentWindow = TimeSpan.FromDays(7);
    public static readonly int LectureLookaheadDays = 14;

    public async Task<StudentDashboardDto> ComposeAsync(Guid studentId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var unavailable = new List<string>();
        var term = await Section("term", unavailable, () => scheduling.GetCurrentTermAsync(cancellationToken));

        var lectures = await Section("today", unavailable, () => LecturesAsync(studentId, term, now, cancellationToken));
        var upcoming = await Section("upcomingAssignments", unavailable, async () =>
            (IReadOnlyList<DashboardAssignmentDto>)[.. (await assignments.GetUpcomingForStudentAsync(studentId, now + AssignmentWindow, cancellationToken))
                .Select(a => new DashboardAssignmentDto(a.Id, a.SubjectId, a.SubjectCode, a.Title, a.DueAt))]);
        var feed = await Section("recentAnnouncements", unavailable, async () =>
            (IReadOnlyList<DashboardAnnouncementDto>)[.. (await announcements.GetStudentFeedAsync(studentId, AnnouncementCount, cancellationToken))
                .Select(a => new DashboardAnnouncementDto(a.Id, a.SubjectId, a.SubjectCode, a.Title, a.Priority, a.IsPinned, a.CreatedAt))]);
        var rate = await Section("attendanceRate", unavailable, () =>
            term is null ? Task.FromResult<decimal?>(null) : attendance.GetStudentRateAsync(studentId, term.Id, cancellationToken));
        var unread = await Section("unreadNotifications", unavailable, async () =>
            (int?)await notifications.CountUnreadAsync(studentId, cancellationToken));
        var fees = await Section("outstandingFees", unavailable, async () =>
            (decimal?)await payments.GetOutstandingBalanceAsync(studentId, cancellationToken));

        if (lectures is null)
            unavailable.Add("nextLecture");

        return new StudentDashboardDto(
            lectures?.Today,
            lectures?.Next,
            upcoming,
            feed,
            rate,
            unread,
            fees,
            unavailable,
            now);
    }

    private async Task<DashboardLectures> LecturesAsync(
        Guid studentId, TermRef? term, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (term is null)
            return new DashboardLectures([], null);

        var groupIds = await lifecycle.GetGroupsForStudentAsync(studentId, term.Id, cancellationToken);
        if (groupIds.Count == 0)
            return new DashboardLectures([], null);

        var zone = await ZoneOfAsync(studentId, cancellationToken);
        var local = TimeZoneInfo.ConvertTime(now, zone);
        var today = DateOnly.FromDateTime(local.DateTime);
        var nowTime = TimeOnly.FromDateTime(local.DateTime);

        var occurrences = (await scheduling.GetOccurrencesAsync(groupIds, today, today.AddDays(LectureLookaheadDays), cancellationToken))
            .OrderBy(o => o.Date)
            .ThenBy(o => o.StartTime)
            .ThenBy(o => o.SubjectCode, StringComparer.Ordinal)
            .Select(o => new DashboardLectureDto(
                o.Date, o.StartTime, o.EndTime, o.SubjectId, o.SubjectCode, o.SubjectName, o.SubjectNameAr, o.GroupNumber, o.FacilityName))
            .ToList();

        // A lecture that is still running counts as the next one: the student is looking for where to be now.
        var next = occurrences.FirstOrDefault(o => o.Date > today || (o.Date == today && o.EndTime > nowTime));
        return new DashboardLectures([.. occurrences.Where(o => o.Date == today)], next);
    }

    private async Task<TimeZoneInfo> ZoneOfAsync(Guid studentId, CancellationToken cancellationToken)
    {
        var placement = await users.GetStudentPlacementAsync(studentId, cancellationToken);
        var settings = await platform.GetEffectiveSettingsAsync(placement?.FacultyId, cancellationToken);
        return TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZone);
    }

    private async Task<T?> Section<T>(string name, List<string> unavailable, Func<Task<T>> read)
    {
        try
        {
            return await read();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Dashboard section {Section} failed; returning it as unavailable.", name);
            unavailable.Add(name);
            return default;
        }
    }
}
