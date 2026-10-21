// ACCEPTANCE TEST — written by the consultant for phase P06. Do not modify; report problems instead.
using System.Net;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Attendance.AttendanceSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Attendance;

/// <summary>P6.3 + P6.4 — audited manual entries and the professor's live roster (rule R7).</summary>
public sealed class ManualAndLiveTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task The_live_roster_starts_with_everyone_not_yet_and_follows_check_ins()
    {
        var w = await Lecture();
        var sessionId = await OpenedAsync(w.HanyClient, w.SlotId);
        var before = await LiveAsync(w.HanyClient, sessionId);

        await CheckedInAsync(w, sessionId, w.AhmedClient, "android-ahmed-7f3a9c21");
        var after = await LiveAsync(w.HanyClient, sessionId);

        Assert.True((bool)before["isOpen"]!);
        Assert.Equal(3, before["items"]!.AsArray().Count);                       // the confirmed members only
        Assert.All(before["items"]!.AsArray(), i => Assert.Equal("not_yet", (string?)i!["status"]));
        Assert.Equal(3, (int)before["counts"]!["notYet"]!);
        Assert.Equal("present", StatusOf(after, w.Ahmed));
        Assert.Equal(1, (int)after["counts"]!["present"]!);
        Assert.Equal(2, (int)after["counts"]!["notYet"]!);
        var codes = after["items"]!.AsArray().Select(i => (string)i!["studentCode"]!).ToList();
        Assert.Equal(codes.Order(StringComparer.Ordinal), codes);
        Assert.All(after["items"]!.AsArray(), i => Assert.Equal("Test Student", (string?)i!["fullName"]));
    }

    [Fact]
    public async Task A_manual_entry_is_stored_as_audited_with_its_note()
    {
        var w = await Lecture();
        var sessionId = await OpenedAsync(w.HanyClient, w.SlotId);

        var response = await ManualAsync(w.HanyClient, sessionId, w.Omar, "present", "هاتفه تعطل — حاضر في القاعة");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var entry = await BodyOf(response);
        Assert.Equal(w.Omar, (Guid)entry["studentId"]!);
        Assert.Equal("manual_audit", (string?)entry["status"]);
        Assert.Equal("manual_entry", (string?)entry["method"]);
        Assert.Equal("هاتفه تعطل — حاضر في القاعة", (string?)entry["note"]);
        Assert.Equal("manual_audit", StatusOf(await LiveAsync(w.HanyClient, sessionId), w.Omar));
    }

    [Fact]
    public async Task A_manual_entry_can_override_a_scan_and_can_mark_absent()
    {
        var w = await Lecture();
        var sessionId = await OpenedAsync(w.HanyClient, w.SlotId);
        Factory.Clock.Advance(TimeSpan.FromMinutes(13));
        await CheckedInAsync(w, sessionId, w.AhmedClient, "android-ahmed-7f3a9c21");     // late

        var excused = await ManualAsync(w.HanyClient, sessionId, w.Ahmed, "present", "Held back by the dean");
        var leftEarly = await ManualAsync(w.HanyClient, sessionId, w.Mona, "absent", "Left after ten minutes");

        Assert.Equal("manual_audit", (string?)(await BodyOf(excused))["status"]);
        Assert.Equal("absent", (string?)(await BodyOf(leftEarly))["status"]);
        var live = await LiveAsync(w.HanyClient, sessionId);
        Assert.Equal("manual_audit", StatusOf(live, w.Ahmed));
        Assert.Equal("absent", StatusOf(live, w.Mona));
    }

    [Fact]
    public async Task A_manual_entry_needs_a_note_and_a_member_of_the_group()
    {
        var w = await Lecture();
        var sessionId = await OpenedAsync(w.HanyClient, w.SlotId);

        var noNote = await ManualAsync(w.HanyClient, sessionId, w.Omar, "present", null);
        var outsider = await ManualAsync(w.HanyClient, sessionId, w.Sara, "present", "Visiting");

        Assert.Equal(HttpStatusCode.BadRequest, noNote.StatusCode);
        Assert.Contains("note", await FieldsOf(noNote));
        Assert.Equal(HttpStatusCode.BadRequest, outsider.StatusCode);
        Assert.Contains("studentId", await FieldsOf(outsider));
    }

    [Fact]
    public async Task Nothing_changes_after_the_session_is_closed()
    {
        var w = await Lecture();
        var sessionId = await OpenedAsync(w.HanyClient, w.SlotId);
        await CloseAsync(w.HanyClient, sessionId);

        var response = await ManualAsync(w.HanyClient, sessionId, w.Omar, "present", "Late paperwork");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Only_staff_teaching_the_group_see_the_roster_or_enter_attendance()
    {
        var w = await Lecture();
        var sessionId = await OpenedAsync(w.HanyClient, w.SlotId);

        Assert.Equal(HttpStatusCode.Forbidden, (await w.LailaClient.GetAsync($"/api/attendance/sessions/{sessionId}/live")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await w.AhmedClient.GetAsync($"/api/attendance/sessions/{sessionId}/live")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await ManualAsync(w.LailaClient, sessionId, w.Omar, "present", "x")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await CloseAsync(w.AhmedClient, sessionId)).StatusCode);
    }

    private Task<LectureWorld> Lecture() => BuildLectureAsync(Factory, Factory.CreateClientAs("admin"));
}
