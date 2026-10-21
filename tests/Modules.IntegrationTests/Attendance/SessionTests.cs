// ACCEPTANCE TEST — written by the consultant for phase P06. Do not modify; report problems instead.
using System.Net;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Attendance.AttendanceSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Attendance;

/// <summary>P6.1 — the teaching professor opens a session and shows a rotating QR (rules R6, phase §7a).</summary>
public sealed class SessionTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task The_teaching_professor_opens_todays_session_by_default()
    {
        var w = await Lecture();

        var response = await OpenSessionAsync(w.HanyClient, w.SlotId);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var session = await BodyOf(response);
        Assert.Equal(w.SlotId, (Guid)session["slotId"]!);
        Assert.Equal(w.GroupId, (Guid)session["subjectGroupId"]!);
        Assert.Equal("CS301", (string?)session["subjectCode"]);
        Assert.Equal(1, (int)session["groupNumber"]!);
        Assert.Equal(Iso(w.Today), (string?)session["sessionDate"]);
        Assert.True((bool)session["isOpen"]!);
        Assert.Null(session["closedAt"]);
    }

    [Fact]
    public async Task A_slot_has_at_most_one_session_per_date_and_only_on_its_own_dates()
    {
        var w = await Lecture();
        await OpenedAsync(w.HanyClient, w.SlotId);

        var again = await OpenSessionAsync(w.HanyClient, w.SlotId, w.Today);
        var tomorrow = await OpenSessionAsync(w.HanyClient, w.SlotId, w.Today.AddDays(1));
        var nextWeek = await OpenSessionAsync(w.HanyClient, w.SlotId, w.Today.AddDays(7));
        var unknownSlot = await OpenSessionAsync(w.HanyClient, Guid.CreateVersion7());

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, tomorrow.StatusCode);
        Assert.Contains("date", await FieldsOf(tomorrow));
        Assert.Equal(HttpStatusCode.Created, nextWeek.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, unknownSlot.StatusCode);
        Assert.Contains("slotId", await FieldsOf(unknownSlot));
    }

    [Fact]
    public async Task The_qr_endpoint_gives_a_token_that_expires_in_about_20_seconds()
    {
        var w = await Lecture();
        var sessionId = await OpenedAsync(w.HanyClient, w.SlotId);

        var response = await w.HanyClient.GetAsync($"/api/attendance/sessions/{sessionId}/qr");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var qr = await BodyOf(response);
        Assert.False(string.IsNullOrWhiteSpace((string?)qr["token"]));
        var expiresIn = DateTimeOffset.Parse((string)qr["expiresAt"]!) - Factory.Clock.GetUtcNow();
        Assert.InRange(expiresIn.TotalSeconds, 15, 21);
    }

    [Fact]
    public async Task Only_staff_teaching_the_group_open_sessions_and_show_the_qr()
    {
        var w = await Lecture();
        var sessionId = await OpenedAsync(w.HanyClient, w.SlotId);

        Assert.Equal(HttpStatusCode.Forbidden, (await OpenSessionAsync(w.LailaClient, w.SlotId, w.Today.AddDays(7))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await OpenSessionAsync(w.AhmedClient, w.SlotId, w.Today.AddDays(7))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await w.LailaClient.GetAsync($"/api/attendance/sessions/{sessionId}/qr")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await w.AhmedClient.GetAsync($"/api/attendance/sessions/{sessionId}/qr")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await OpenSessionAsync(Factory.CreateClient(), w.SlotId)).StatusCode);
    }

    [Fact]
    public async Task A_closed_session_shows_no_more_qr_codes()
    {
        var w = await Lecture();
        var sessionId = await OpenedAsync(w.HanyClient, w.SlotId);
        await CloseAsync(w.HanyClient, sessionId);

        var response = await w.HanyClient.GetAsync($"/api/attendance/sessions/{sessionId}/qr");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private Task<LectureWorld> Lecture() => BuildLectureAsync(Factory, Factory.CreateClientAs("admin"));
}
