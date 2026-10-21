// ACCEPTANCE TEST — written by the consultant for phase P06. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Attendance.AttendanceSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Attendance;

/// <summary>P6.2 — students check in by scanning the QR from their one registered phone (rules R1–R5, DEVICE).</summary>
public sealed class CheckInTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    private const string AhmedPhone = "android-ahmed-7f3a9c21";

    [Fact]
    public async Task Scanning_within_the_grace_period_is_present()
    {
        var w = await Lecture();
        var sessionId = await OpenedAsync(w.HanyClient, w.SlotId);

        var response = await CheckInAsync(w.AhmedClient, await QrAsync(w.HanyClient, sessionId), AhmedPhone);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await BodyOf(response);
        Assert.Equal(sessionId, (Guid)result["sessionId"]!);
        Assert.Equal("present", (string?)result["status"]);
        Assert.Equal("CS301", (string?)result["subjectCode"]);
        Assert.NotNull((string?)result["checkedInAt"]);
    }

    [Fact]
    public async Task Scanning_after_the_grace_period_is_late()
    {
        var w = await Lecture();
        var sessionId = await OpenedAsync(w.HanyClient, w.SlotId);

        Factory.Clock.Advance(TimeSpan.FromMinutes(13));                  // lecture started 2 min ago; grace is 10
        var result = await CheckedInAsync(w, sessionId, w.AhmedClient, AhmedPhone);

        Assert.Equal("late", (string?)result["status"]);
    }

    [Fact]
    public async Task The_grace_period_comes_from_the_faculty_settings()
    {
        var w = await Lecture();
        var url = $"/api/admin/faculties/{w.FacultyId}/settings";
        var current = (await Factory.CreateClientAs("admin").GetFromJsonAsync<JsonObject>(url))!;   // overrides exist → send the version
        var faculty = await Factory.CreateClientAs("admin").PutAsJsonAsync(url,
            new { workingDays = new[] { 0, 1, 2, 3, 4, 5, 6 }, lateGraceMinutes = 30, version = (uint)current["version"]! });
        Assert.Equal(HttpStatusCode.OK, faculty.StatusCode);
        var sessionId = await OpenedAsync(w.HanyClient, w.SlotId);

        Factory.Clock.Advance(TimeSpan.FromMinutes(13));
        var result = await CheckedInAsync(w, sessionId, w.AhmedClient, AhmedPhone);

        Assert.Equal("present", (string?)result["status"]);
    }

    [Fact]
    public async Task A_qr_code_older_than_20_seconds_has_expired()
    {
        var w = await Lecture();
        var sessionId = await OpenedAsync(w.HanyClient, w.SlotId);
        var token = await QrAsync(w.HanyClient, sessionId);

        Factory.Clock.Advance(TimeSpan.FromSeconds(21));
        var response = await CheckInAsync(w.AhmedClient, token, AhmedPhone);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("token", await FieldsOf(response));
        Assert.Contains("The QR code has expired.", await ErrorTextOf(response));
    }

    [Fact]
    public async Task A_forged_or_garbled_qr_code_is_not_valid()
    {
        var w = await Lecture();
        var sessionId = await OpenedAsync(w.HanyClient, w.SlotId);
        var token = await QrAsync(w.HanyClient, sessionId);
        var middle = token.Length / 2;
        var forged = token[..middle] + (token[middle] == 'A' ? 'B' : 'A') + token[(middle + 1)..];

        var forgedResponse = await CheckInAsync(w.AhmedClient, forged, AhmedPhone);
        var garbage = await CheckInAsync(w.AhmedClient, "not-a-qr-code", AhmedPhone);

        Assert.Equal(HttpStatusCode.BadRequest, forgedResponse.StatusCode);
        Assert.Contains("The QR code is not valid.", await ErrorTextOf(forgedResponse));
        Assert.Equal(HttpStatusCode.BadRequest, garbage.StatusCode);
        Assert.Contains("The QR code is not valid.", await ErrorTextOf(garbage));
    }

    [Fact]
    public async Task A_student_outside_the_group_is_refused()
    {
        var w = await Lecture();
        var sessionId = await OpenedAsync(w.HanyClient, w.SlotId);

        var response = await CheckInAsync(w.SaraClient, await QrAsync(w.HanyClient, sessionId), "android-sara-11aa22bb");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("You are not in this group.", await ErrorTextOf(response));
    }

    [Fact]
    public async Task Checking_in_twice_is_a_409()
    {
        var w = await Lecture();
        var sessionId = await OpenedAsync(w.HanyClient, w.SlotId);
        await CheckedInAsync(w, sessionId, w.AhmedClient, AhmedPhone);

        var again = await CheckInAsync(w.AhmedClient, await QrAsync(w.HanyClient, sessionId), AhmedPhone);

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Contains("Already checked in.", await ErrorTextOf(again));
    }

    [Fact]
    public async Task The_first_phone_used_becomes_the_registered_device()
    {
        var w = await Lecture();
        var today = await OpenedAsync(w.HanyClient, w.SlotId);
        await CheckedInAsync(w, today, w.AhmedClient, AhmedPhone);
        var nextWeek = await OpenedAsync(w.HanyClient, w.SlotId, w.Today.AddDays(7));

        var friendsPhone = await CheckInAsync(w.AhmedClient, await QrAsync(w.HanyClient, nextWeek), "android-friend-99887766");
        var ownPhone = await CheckInAsync(w.AhmedClient, await QrAsync(w.HanyClient, nextWeek), AhmedPhone);

        Assert.Equal(HttpStatusCode.Forbidden, friendsPhone.StatusCode);
        Assert.Contains("Use your registered device.", await ErrorTextOf(friendsPhone));
        Assert.Equal(HttpStatusCode.OK, ownPhone.StatusCode);
    }

    [Fact]
    public async Task A_closed_session_accepts_no_check_ins()
    {
        var w = await Lecture();
        var sessionId = await OpenedAsync(w.HanyClient, w.SlotId);
        var token = await QrAsync(w.HanyClient, sessionId);
        await CloseAsync(w.HanyClient, sessionId);

        var response = await CheckInAsync(w.AhmedClient, token, AhmedPhone);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("The session is closed.", await ErrorTextOf(response));
    }

    [Fact]
    public async Task Only_students_check_in()
    {
        var w = await Lecture();
        var sessionId = await OpenedAsync(w.HanyClient, w.SlotId);

        var professor = await CheckInAsync(w.HanyClient, await QrAsync(w.HanyClient, sessionId), "android-hany-00112233");

        Assert.Equal(HttpStatusCode.Forbidden, professor.StatusCode);
    }

    private Task<LectureWorld> Lecture() => BuildLectureAsync(Factory, Factory.CreateClientAs("admin"));
}
