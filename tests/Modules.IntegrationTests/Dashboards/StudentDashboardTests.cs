// ACCEPTANCE TEST — written by the consultant for phase P10. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Attendance;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Assignments.AssignmentsSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Attendance.AttendanceSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Payments.PaymentsSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Dashboards;

/// <summary>
/// P10.1 — one call for the student's home screen, composed in the host from module contracts only. A section whose
/// source fails is null and named in <c>unavailable</c>; an empty section is an empty list, 0 or null by meaning.
/// </summary>
public sealed class StudentDashboardTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task The_dashboard_combines_every_section()
    {
        var w = await BuildLectureAsync(Factory, Admin());
        var now = Factory.Clock.GetUtcNow();
        var soon = await CreatedAsync(w.HanyClient, w.SubjectId, now.AddDays(3), "Lab 3 — Heaps");
        await CreatedAsync(w.HanyClient, w.SubjectId, now.AddDays(10), "Project");                  // beyond 7 days
        var submitted = await CreatedAsync(w.HanyClient, w.SubjectId, now.AddDays(2), "Lab 2 — Graphs");
        await SubmittedAsync(w.AhmedClient, submitted);                                              // already handed in
        var posted = await w.HanyClient.PostAsJsonAsync("/api/announcements", new
        {
            subjectId = w.SubjectId, subjectGroupId = (Guid?)null, title = "Quiz on Sunday", body = "Chapters 3–4.",
            priority = "high", isPinned = false, expiresAt = (DateTimeOffset?)null,
        });
        Assert.Equal(HttpStatusCode.Created, posted.StatusCode);
        await CreateFeeItemAsync(Admin(), "Tuition", "tuition", 500m, w.TermId, nameAr: "المصروفات الدراسية");
        var sessionId = await OpenedAsync(w.HanyClient, w.SlotId);
        Assert.Equal(HttpStatusCode.OK, (await ManualAsync(w.HanyClient, sessionId, w.Ahmed, "present", "Phone broken")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await CloseAsync(w.HanyClient, sessionId)).StatusCode);

        var dashboard = await DashboardAsync(w.AhmedClient);

        Assert.Empty(dashboard["unavailable"]!.AsArray());
        var today = Assert.Single(dashboard["today"]!.AsArray())!;
        Assert.Equal("CS301", (string?)today["subjectCode"]);
        Assert.Equal(w.Today.ToString("yyyy-MM-dd"), (string?)today["date"]);
        Assert.Equal("Hall A", (string?)today["facilityName"]);
        Assert.Equal("CS301", (string?)dashboard["nextLecture"]!["subjectCode"]);                 // still running counts
        var upcoming = Assert.Single(dashboard["upcomingAssignments"]!.AsArray())!;
        Assert.Equal(soon, (Guid)upcoming["id"]!);
        Assert.Equal("Lab 3 — Heaps", (string?)upcoming["title"]);
        Assert.Equal("CS301", (string?)upcoming["subjectCode"]);
        var announcement = Assert.Single(dashboard["recentAnnouncements"]!.AsArray())!;
        Assert.Equal("Quiz on Sunday", (string?)announcement["title"]);
        Assert.Equal("high", (string?)announcement["priority"]);
        Assert.Equal(100m, (decimal)dashboard["attendanceRate"]!);
        Assert.Equal(500m, (decimal)dashboard["outstandingFees"]!);
        Assert.Equal(await UnreadAsync(w.AhmedClient), (int)dashboard["unreadNotifications"]!);
    }

    [Fact]
    public async Task A_student_with_nothing_yet_gets_empty_sections_not_errors()
    {
        var w = await BuildLectureAsync(Factory, Admin());

        var dashboard = await DashboardAsync(w.SaraClient);                                            // not registered anywhere

        Assert.Empty(dashboard["unavailable"]!.AsArray());
        Assert.Empty(dashboard["today"]!.AsArray());
        Assert.Null(dashboard["nextLecture"]);
        Assert.Empty(dashboard["upcomingAssignments"]!.AsArray());
        Assert.Empty(dashboard["recentAnnouncements"]!.AsArray());
        Assert.Null(dashboard["attendanceRate"]);
        Assert.Equal(0m, (decimal)dashboard["outstandingFees"]!);
        Assert.Equal(await UnreadAsync(w.SaraClient), (int)dashboard["unreadNotifications"]!);
    }

    [Fact]
    public async Task A_missed_lecture_lowers_the_attendance_rate()
    {
        var w = await BuildLectureAsync(Factory, Admin());
        var sessionId = await OpenedAsync(w.HanyClient, w.SlotId);
        Assert.Equal(HttpStatusCode.OK, (await CloseAsync(w.HanyClient, sessionId)).StatusCode);   // Mona is auto-absent

        Assert.Equal(0m, (decimal)(await DashboardAsync(w.MonaClient))["attendanceRate"]!);
    }

    [Fact]
    public async Task Only_students_have_a_dashboard()
    {
        var w = await BuildLectureAsync(Factory, Admin());

        Assert.Equal(HttpStatusCode.Forbidden, (await w.HanyClient.GetAsync("/api/dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Admin().GetAsync("/api/dashboard")).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private static async Task<JsonObject> DashboardAsync(HttpClient student)
    {
        var response = await student.GetAsync("/api/dashboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonObject>())!;
    }

    private static async Task<int> UnreadAsync(HttpClient client) =>
        (int)(await client.GetFromJsonAsync<JsonObject>("/api/notifications/mine/unread-count"))!["count"]!;
}
