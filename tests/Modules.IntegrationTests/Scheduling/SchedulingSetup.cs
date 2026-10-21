// ACCEPTANCE TEST — written by the consultant for phase P03. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace NIMI.UniPortal.Modules.IntegrationTests.Scheduling;

/// <summary>
/// Builds scheduling data through the real admin API. Calendar used everywhere: the 2026/2027 first term runs
/// Saturday 2026-09-19 → Friday 2027-01-15. Product days: 0 = Saturday, 1 = Sunday … 5 = Thursday, 6 = Friday.
/// Later phases (registration, attendance, materials…) reuse these helpers.
/// </summary>
internal static class SchedulingSetup
{
    public const string TermStart = "2026-09-19";
    public const string TermEnd = "2027-01-15";

    public static async Task<HttpResponseMessage> PostTermAsync(
        HttpClient admin, int academicYear = 2026, string number = "first",
        string startsOn = TermStart, string endsOn = TermEnd, int weekCount = 17) =>
        await admin.PostAsJsonAsync("/api/admin/terms", new { academicYear, number, startsOn, endsOn, weekCount });

    public static async Task<Guid> CreateTermAsync(
        HttpClient admin, int academicYear = 2026, string number = "first",
        string startsOn = TermStart, string endsOn = TermEnd, int weekCount = 17) =>
        await IdOf(await PostTermAsync(admin, academicYear, number, startsOn, endsOn, weekCount));

    public static async Task<Guid> CreateActiveTermAsync(HttpClient admin)
    {
        var termId = await CreateTermAsync(admin);
        var response = await admin.PostAsync($"/api/admin/terms/{termId}/activate", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return termId;
    }

    public static async Task<HttpResponseMessage> PostSubjectAsync(
        HttpClient admin, Guid departmentId, string code, string name = "Algorithms", string nameAr = "الخوارزميات",
        int creditHours = 3, string category = "regular", bool isGraduationRequirement = false) =>
        await admin.PostAsJsonAsync("/api/admin/subjects", new
        {
            departmentId, code, name, nameAr, description = (string?)null, creditHours, category, isGraduationRequirement,
        });

    public static async Task<Guid> CreateSubjectAsync(
        HttpClient admin, Guid departmentId, string code, string name = "Algorithms", string nameAr = "الخوارزميات", int creditHours = 3) =>
        await IdOf(await PostSubjectAsync(admin, departmentId, code, name, nameAr, creditHours));

    public static async Task<HttpResponseMessage> PostGroupAsync(
        HttpClient admin, Guid subjectId, Guid termId, int groupNumber = 1, int capacity = 60, Guid? targetDepartmentId = null) =>
        await admin.PostAsJsonAsync($"/api/admin/subjects/{subjectId}/groups", new { termId, groupNumber, targetDepartmentId, capacity });

    public static async Task<Guid> CreateGroupAsync(HttpClient admin, Guid subjectId, Guid termId, int groupNumber = 1, int capacity = 60) =>
        await IdOf(await PostGroupAsync(admin, subjectId, termId, groupNumber, capacity));

    public static async Task<HttpResponseMessage> PostAssignmentAsync(
        HttpClient admin, Guid staffId, Guid subjectId, Guid termId, Guid? subjectGroupId = null, string role = "lecturer") =>
        await admin.PostAsJsonAsync("/api/admin/teaching-assignments", new { staffId, subjectId, termId, subjectGroupId, role });

    public static async Task<Guid> AssignTeacherAsync(
        HttpClient admin, Guid staffId, Guid subjectId, Guid termId, Guid? subjectGroupId = null, string role = "lecturer") =>
        await IdOf(await PostAssignmentAsync(admin, staffId, subjectId, termId, subjectGroupId, role));

    public static async Task<HttpResponseMessage> PostFacilityAsync(
        HttpClient admin, string name, Guid? facultyId = null, string type = "lecture_hall", int capacity = 120) =>
        await admin.PostAsJsonAsync("/api/admin/facilities", new { facultyId, name, type, capacity, building = "Main building", floor = "1" });

    public static async Task<Guid> CreateFacilityAsync(HttpClient admin, string name, Guid? facultyId = null) =>
        await IdOf(await PostFacilityAsync(admin, name, facultyId));

    public static async Task<HttpResponseMessage> PostSlotAsync(
        HttpClient admin, Guid groupId, Guid facilityId, Guid? staffId, int dayOfWeek, string startTime, string endTime,
        string recurrence = "weekly", string validFrom = TermStart, string? validUntil = null) =>
        await admin.PostAsJsonAsync("/api/admin/schedules", SlotBody(groupId, facilityId, staffId, dayOfWeek, startTime, endTime, recurrence, validFrom, validUntil));

    public static async Task<Guid> CreateSlotAsync(
        HttpClient admin, Guid groupId, Guid facilityId, Guid? staffId, int dayOfWeek, string startTime, string endTime,
        string recurrence = "weekly", string validFrom = TermStart, string? validUntil = null) =>
        await IdOf(await PostSlotAsync(admin, groupId, facilityId, staffId, dayOfWeek, startTime, endTime, recurrence, validFrom, validUntil));

    public static object SlotBody(
        Guid groupId, Guid facilityId, Guid? staffId, int dayOfWeek, string startTime, string endTime,
        string recurrence = "weekly", string validFrom = TermStart, string? validUntil = null) =>
        new { subjectGroupId = groupId, facilityId, staffId, dayOfWeek, startTime, endTime, recurrence, validFrom, validUntil };

    public static async Task<JsonObject> BodyOf(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonObject>())!;

    public static async Task<List<string?>> FieldsOf(HttpResponseMessage response) =>
        (await BodyOf(response))["details"]!.AsArray().Select(d => (string?)d!["field"]).ToList();

    private static async Task<Guid> IdOf(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (Guid)(await BodyOf(response))["id"]!;
    }
}
