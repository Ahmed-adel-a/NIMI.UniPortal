// ACCEPTANCE TEST — written by the consultant for phase P06. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Attendance.AttendanceSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Attendance;

/// <summary>P6.8 — changing the registered phone goes through Student Affairs, at most once per term (DEVICE, R11).</summary>
public sealed class DeviceChangeTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    private const string OldPhone = "android-ahmed-7f3a9c21";
    private const string NewPhone = "iphone-ahmed-0b44e1aa";

    [Fact]
    public async Task An_approved_change_swaps_the_registered_phone()
    {
        var w = await Lecture();
        var today = await OpenedAsync(w.HanyClient, w.SlotId);
        await CheckedInAsync(w, today, w.AhmedClient, OldPhone);

        var request = await RequestAsync(w.AhmedClient, NewPhone, "My phone was stolen");
        Assert.Equal(HttpStatusCode.Created, request.StatusCode);
        var created = await BodyOf(request);
        Assert.Equal("pending", (string?)created["status"]);
        var pending = (await Admin().GetFromJsonAsync<JsonObject>("/api/admin/attendance/device-changes?status=pending"))!;
        var listed = pending["items"]!.AsArray().Single()!;
        Assert.Equal(w.Ahmed, (Guid)listed["studentId"]!);
        Assert.Equal("Test Student", (string?)listed["studentName"]);
        Assert.Equal(NewPhone, (string?)listed["newFingerprint"]);
        Assert.Equal("My phone was stolen", (string?)listed["reason"]);

        var approve = await Admin().PostAsync($"/api/admin/attendance/device-changes/{(Guid)created["id"]!}/approve", null);

        Assert.Equal(HttpStatusCode.OK, approve.StatusCode);
        Assert.Equal("approved", (string?)(await BodyOf(approve))["status"]);
        var nextWeek = await OpenedAsync(w.HanyClient, w.SlotId, w.Today.AddDays(7));
        var withOld = await CheckInAsync(w.AhmedClient, await QrAsync(w.HanyClient, nextWeek), OldPhone);
        var withNew = await CheckInAsync(w.AhmedClient, await QrAsync(w.HanyClient, nextWeek), NewPhone);
        Assert.Equal(HttpStatusCode.Forbidden, withOld.StatusCode);
        Assert.Equal(HttpStatusCode.OK, withNew.StatusCode);
    }

    [Fact]
    public async Task A_rejected_change_keeps_the_old_phone()
    {
        var w = await Lecture();
        var today = await OpenedAsync(w.HanyClient, w.SlotId);
        await CheckedInAsync(w, today, w.AhmedClient, OldPhone);
        var id = (Guid)(await BodyOf(await RequestAsync(w.AhmedClient, NewPhone, "I prefer my tablet")))["id"]!;

        var reject = await Admin().PostAsync($"/api/admin/attendance/device-changes/{id}/reject", null);
        var decideAgain = await Admin().PostAsync($"/api/admin/attendance/device-changes/{id}/approve", null);

        Assert.Equal("rejected", (string?)(await BodyOf(reject))["status"]);
        Assert.Equal(HttpStatusCode.Conflict, decideAgain.StatusCode);
        var nextWeek = await OpenedAsync(w.HanyClient, w.SlotId, w.Today.AddDays(7));
        Assert.Equal(HttpStatusCode.OK, (await CheckInAsync(w.AhmedClient, await QrAsync(w.HanyClient, nextWeek), OldPhone)).StatusCode);
    }

    [Fact]
    public async Task One_pending_request_at_a_time_and_one_approved_change_per_term()
    {
        var w = await Lecture();
        var first = (Guid)(await BodyOf(await RequestAsync(w.AhmedClient, NewPhone, "Stolen")))["id"]!;

        var whilePending = await RequestAsync(w.AhmedClient, "android-ahmed-ffff0000", "Changed my mind");
        await Admin().PostAsync($"/api/admin/attendance/device-changes/{first}/approve", null);
        var afterApproval = await RequestAsync(w.AhmedClient, "android-ahmed-ffff0000", "Broke the new one");

        Assert.Equal(HttpStatusCode.Conflict, whilePending.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, afterApproval.StatusCode);
        Assert.Contains("newFingerprint", await FieldsOf(afterApproval));
    }

    [Fact]
    public async Task Requests_need_a_reason_and_a_sane_fingerprint()
    {
        var w = await Lecture();

        var noReason = await RequestAsync(w.AhmedClient, NewPhone, "");
        var shortPrint = await RequestAsync(w.AhmedClient, "abc", "Stolen");

        Assert.Contains("reason", await FieldsOf(noReason));
        Assert.Contains("newFingerprint", await FieldsOf(shortPrint));
    }

    [Fact]
    public async Task Only_students_request_and_only_admins_decide()
    {
        var w = await Lecture();
        var id = (Guid)(await BodyOf(await RequestAsync(w.AhmedClient, NewPhone, "Stolen")))["id"]!;

        Assert.Equal(HttpStatusCode.Forbidden, (await RequestAsync(w.HanyClient, NewPhone, "Stolen")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await w.AhmedClient.GetAsync("/api/admin/attendance/device-changes")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await w.HanyClient.PostAsync($"/api/admin/attendance/device-changes/{id}/approve", null)).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private Task<LectureWorld> Lecture() => BuildLectureAsync(Factory, Admin());

    private static async Task<HttpResponseMessage> RequestAsync(HttpClient student, string newFingerprint, string reason) =>
        await student.PostAsJsonAsync("/api/attendance/device-change", new { newFingerprint, reason });
}
