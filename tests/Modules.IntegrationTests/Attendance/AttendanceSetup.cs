// ACCEPTANCE TEST — written by the consultant for phase P06. Do not modify; report problems instead.
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Users;
using static NIMI.UniPortal.Modules.IntegrationTests.Lifecycle.RegistrationSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Attendance;

/// <summary>Attendance helpers through the real API. Builds a lecture that is happening <b>now</b> (Cairo time).</summary>
internal static class AttendanceSetup
{
    public static readonly TimeZoneInfo Cairo = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");

    public static string Iso(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static async Task<HttpResponseMessage> OpenSessionAsync(HttpClient teacher, Guid slotId, DateOnly? date = null) =>
        await teacher.PostAsJsonAsync("/api/attendance/sessions", new { slotId, date = date is { } d ? Iso(d) : null });

    public static async Task<Guid> OpenedAsync(HttpClient teacher, Guid slotId, DateOnly? date = null)
    {
        var response = await OpenSessionAsync(teacher, slotId, date);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (Guid)(await BodyOf(response))["id"]!;
    }

    public static async Task<string> QrAsync(HttpClient teacher, Guid sessionId)
    {
        var response = await teacher.GetAsync($"/api/attendance/sessions/{sessionId}/qr");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (string)(await BodyOf(response))["token"]!;
    }

    public static async Task<HttpResponseMessage> CheckInAsync(HttpClient student, string token, string deviceFingerprint) =>
        await student.PostAsJsonAsync("/api/attendance/check-in", new { token, deviceFingerprint });

    /// <summary>Gets a fresh QR from the teacher and checks the student in with it; asserts 200.</summary>
    public static async Task<JsonObject> CheckedInAsync(LectureWorld w, Guid sessionId, HttpClient student, string deviceFingerprint)
    {
        var response = await CheckInAsync(student, await QrAsync(w.HanyClient, sessionId), deviceFingerprint);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await BodyOf(response);
    }

    public static async Task<HttpResponseMessage> ManualAsync(HttpClient teacher, Guid sessionId, Guid studentId, string status, string? note) =>
        await teacher.PostAsJsonAsync($"/api/attendance/sessions/{sessionId}/manual", new { studentId, status, note });

    public static async Task<HttpResponseMessage> CloseAsync(HttpClient teacher, Guid sessionId) =>
        await teacher.PostAsync($"/api/attendance/sessions/{sessionId}/close", null);

    public static async Task<JsonObject> LiveAsync(HttpClient teacher, Guid sessionId) =>
        (await teacher.GetFromJsonAsync<JsonObject>($"/api/attendance/sessions/{sessionId}/live"))!;

    public static string? StatusOf(JsonObject live, Guid studentId) =>
        (string?)live["items"]!.AsArray().Single(i => (Guid)i!["studentId"]! == studentId)!["status"];

    /// <summary>The top-level <c>message</c> plus every detail <c>error</c> of an error response.</summary>
    public static async Task<string> ErrorTextOf(HttpResponseMessage response)
    {
        var body = await BodyOf(response);
        var details = body["details"]?.AsArray().Select(d => (string?)d!["error"]) ?? [];
        return string.Join(" | ", new[] { (string?)body["message"] }.Concat(details));
    }

    public static async Task<LectureWorld> BuildLectureAsync(PortalWebAppFactory factory, HttpClient admin)
    {
        var local = TimeZoneInfo.ConvertTime(factory.Clock.GetUtcNow(), Cairo);
        var today = DateOnly.FromDateTime(local.DateTime);
        var day = ((int)today.DayOfWeek + 1) % 7;
        var minuteOfDay = Math.Max(0, local.Hour * 60 + local.Minute - 2);           // started 2 minutes ago
        var start = new TimeOnly(minuteOfDay / 60, minuteOfDay % 60);
        var endMinute = Math.Min(minuteOfDay + 90, 23 * 60 + 59);
        var end = new TimeOnly(endMinute / 60, endMinute % 60);

        var eng = await OrganizationSetup.CreateFacultyAsync(admin, "ENG");
        var cs = await OrganizationSetup.CreateDepartmentAsync(admin, eng, "CS");
        var allDays = await admin.PutAsJsonAsync($"/api/admin/faculties/{eng}/settings", new { workingDays = new[] { 0, 1, 2, 3, 4, 5, 6 } });
        Assert.Equal(HttpStatusCode.OK, allDays.StatusCode);

        var termId = await CreateTermAsync(admin, academicYear: today.Year, number: "first",
            startsOn: Iso(today.AddDays(-30)), endsOn: Iso(today.AddDays(60)), weekCount: 13);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/admin/terms/{termId}/activate", null)).StatusCode);
        await OpenWindowAsync(factory, admin, termId, minCredits: 0);

        var subjectId = await CreateSubjectAsync(admin, cs, "CS301", "Algorithms", "الخوارزميات");
        var groupId = await CreateGroupAsync(admin, subjectId, termId, 1);
        var hall = await CreateFacilityAsync(admin, "Hall A", eng);
        var slotId = await CreateSlotAsync(admin, groupId, hall, null, day,
            start.ToString("HH:mm:ss", CultureInfo.InvariantCulture), end.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            validFrom: Iso(today.AddDays(-30)));

        var (hany, hanyToken) = await AccountSetup.CreateProfessorWithTokenAsync(factory, admin, cs, "hany@cu.edu.eg");
        var (_, lailaToken) = await AccountSetup.CreateProfessorWithTokenAsync(factory, admin, cs, "laila@cu.edu.eg");
        await AssignTeacherAsync(admin, hany, subjectId, termId);

        var (ahmed, ahmedToken) = await AccountSetup.CreateStudentWithTokenAsync(factory, admin, cs, "ahmed@cu.edu.eg");
        var (mona, monaToken) = await AccountSetup.CreateStudentWithTokenAsync(factory, admin, cs, "mona@cu.edu.eg");
        var (omar, omarToken) = await AccountSetup.CreateStudentWithTokenAsync(factory, admin, cs, "omar@cu.edu.eg");
        var (sara, saraToken) = await AccountSetup.CreateStudentWithTokenAsync(factory, admin, cs, "sara@cu.edu.eg");
        var ahmedClient = factory.CreateClientWithToken(ahmedToken);
        var monaClient = factory.CreateClientWithToken(monaToken);
        var omarClient = factory.CreateClientWithToken(omarToken);
        foreach (var student in new[] { ahmedClient, monaClient, omarClient })
            await RegisteredAsync(student, groupId);

        return new LectureWorld(
            FacultyId: eng, DepartmentId: cs, TermId: termId, SubjectId: subjectId, GroupId: groupId, SlotId: slotId, Today: today,
            Hany: hany, HanyClient: factory.CreateClientWithToken(hanyToken), LailaClient: factory.CreateClientWithToken(lailaToken),
            Ahmed: ahmed, AhmedClient: ahmedClient, Mona: mona, MonaClient: monaClient, Omar: omar, OmarClient: omarClient,
            Sara: sara, SaraClient: factory.CreateClientWithToken(saraToken));
    }
}
