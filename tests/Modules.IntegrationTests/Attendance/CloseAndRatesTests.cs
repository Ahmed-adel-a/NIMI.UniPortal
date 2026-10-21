// ACCEPTANCE TEST — written by the consultant for phase P06. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Attendance.AttendanceSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Attendance;

/// <summary>P6.5–P6.7 — closing fills the absents; percentages, at-risk lists and one warning per drop (R8–R10).</summary>
public sealed class CloseAndRatesTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task Closing_marks_everyone_without_a_record_absent_and_is_idempotent()
    {
        var w = await Lecture();
        var sessionId = await HeldSessionAsync(w);                          // Ahmed scanned, Mona manual, Omar nothing

        var first = await CloseAsync(w.HanyClient, sessionId);
        var second = await CloseAsync(w.HanyClient, sessionId);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var summary = await BodyOf(first);
        Assert.Equal(1, (int)summary["present"]!);
        Assert.Equal(0, (int)summary["late"]!);
        Assert.Equal(1, (int)summary["manualAudit"]!);
        Assert.Equal(1, (int)summary["absent"]!);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal((string?)summary["closedAt"], (string?)(await BodyOf(second))["closedAt"]);
        var live = await LiveAsync(w.HanyClient, sessionId);
        Assert.False((bool)live["isOpen"]!);
        Assert.Equal("absent", StatusOf(live, w.Omar));
    }

    [Fact]
    public async Task Students_see_their_own_history_with_a_percentage_per_subject()
    {
        var w = await Lecture();
        await CloseAsync(w.HanyClient, await HeldSessionAsync(w));

        var ahmed = await HistoryAsync(w.AhmedClient);
        var omar = await HistoryAsync(w.OmarClient);

        Assert.Equal(1, (int)ahmed["totalCount"]!);
        Assert.Equal("present", (string?)ahmed["items"]![0]!["status"]);
        Assert.Equal("CS301", (string?)ahmed["items"]![0]!["subjectCode"]);
        Assert.Equal(Iso(w.Today), (string?)ahmed["items"]![0]!["sessionDate"]);
        var ahmedRate = ahmed["subjects"]!.AsArray().Single()!;
        Assert.Equal(1, (int)ahmedRate["attended"]!);
        Assert.Equal(1, (int)ahmedRate["held"]!);
        Assert.Equal(100m, (decimal)ahmedRate["percentage"]!);
        Assert.Equal("absent", (string?)omar["items"]![0]!["status"]);
        Assert.Equal(0m, (decimal)omar["subjects"]![0]!["percentage"]!);
    }

    [Fact]
    public async Task The_teacher_sees_a_percentage_per_student_and_others_cannot()
    {
        var w = await Lecture();
        await CloseAsync(w.HanyClient, await HeldSessionAsync(w));

        var summary = (await w.HanyClient.GetFromJsonAsync<JsonObject>($"/api/attendance/groups/{w.GroupId}/summary"))!;

        Assert.Equal(3, (int)summary["totalCount"]!);
        var byStudent = summary["items"]!.AsArray().ToDictionary(i => (Guid)i!["studentId"]!, i => (decimal)i!["percentage"]!);
        Assert.Equal(100m, byStudent[w.Ahmed]);
        Assert.Equal(100m, byStudent[w.Mona]);                              // manual_audit counts as attended
        Assert.Equal(0m, byStudent[w.Omar]);
        Assert.Equal(HttpStatusCode.OK, (await Factory.CreateClientAs("admin").GetAsync($"/api/attendance/groups/{w.GroupId}/summary")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await w.LailaClient.GetAsync($"/api/attendance/groups/{w.GroupId}/summary")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await w.AhmedClient.GetAsync($"/api/attendance/groups/{w.GroupId}/summary")).StatusCode);
    }

    [Fact]
    public async Task Students_below_the_threshold_are_listed_at_risk_and_warned_once()
    {
        var w = await Lecture();
        await CloseAsync(w.HanyClient, await HeldSessionAsync(w));
        var atRisk = (await w.HanyClient.GetFromJsonAsync<JsonObject>($"/api/attendance/at-risk?groupId={w.GroupId}"))!;
        var omarAfterFirst = await UnreadAsync(w.OmarClient);

        var nextWeek = await OpenedAsync(w.HanyClient, w.SlotId, w.Today.AddDays(7));      // nobody checks in
        await CloseAsync(w.HanyClient, nextWeek);

        Assert.Equal([w.Omar], atRisk["items"]!.AsArray().Select(i => (Guid)i!["studentId"]!));
        Assert.Equal(75m, (decimal)atRisk["items"]![0]!["threshold"]!);
        Assert.Equal(0m, (decimal)atRisk["items"]![0]!["percentage"]!);
        Assert.Equal("CS301", (string?)atRisk["items"]![0]!["subjectCode"]);
        Assert.Equal(1, omarAfterFirst);
        Assert.Equal(1, await UnreadAsync(w.OmarClient));                   // still below: no second warning
        Assert.Equal(1, await UnreadAsync(w.AhmedClient));                  // 1 of 2 = 50 % → newly below → warned
        Assert.Equal(1, await UnreadAsync(w.MonaClient));
    }

    [Fact]
    public async Task The_threshold_comes_from_the_faculty_settings()
    {
        var w = await Lecture();
        var faculty = await Factory.CreateClientAs("admin").PutAsJsonAsync($"/api/admin/faculties/{w.FacultyId}/settings",
            new { workingDays = new[] { 0, 1, 2, 3, 4, 5, 6 }, attendanceThreshold = 40 });
        Assert.Equal(HttpStatusCode.OK, faculty.StatusCode);
        await CloseAsync(w.HanyClient, await HeldSessionAsync(w));
        var nextWeek = await OpenedAsync(w.HanyClient, w.SlotId, w.Today.AddDays(7));
        await CloseAsync(w.HanyClient, nextWeek);

        var atRisk = (await Factory.CreateClientAs("admin").GetFromJsonAsync<JsonObject>($"/api/attendance/at-risk?termId={w.TermId}"))!;

        Assert.Equal([w.Omar], atRisk["items"]!.AsArray().Select(i => (Guid)i!["studentId"]!));   // Ahmed at 50 % is above 40 %
        Assert.Equal(40m, (decimal)atRisk["items"]![0]!["threshold"]!);
        Assert.Equal(0, await UnreadAsync(w.AhmedClient));
    }

    private Task<LectureWorld> Lecture() => BuildLectureAsync(Factory, Factory.CreateClientAs("admin"));

    /// <summary>Today's session: Ahmed scans on time, Mona is entered manually, Omar does nothing.</summary>
    private async Task<Guid> HeldSessionAsync(LectureWorld w)
    {
        var sessionId = await OpenedAsync(w.HanyClient, w.SlotId);
        await CheckedInAsync(w, sessionId, w.AhmedClient, "android-ahmed-7f3a9c21");
        Assert.Equal(HttpStatusCode.OK, (await ManualAsync(w.HanyClient, sessionId, w.Mona, "present", "Scanner queue too long")).StatusCode);
        return sessionId;
    }

    private static async Task<JsonObject> HistoryAsync(HttpClient student) =>
        (await student.GetFromJsonAsync<JsonObject>("/api/attendance/history"))!;

    private static async Task<int> UnreadAsync(HttpClient client) =>
        (int)(await client.GetFromJsonAsync<JsonObject>("/api/notifications/mine/unread-count"))!["count"]!;
}
