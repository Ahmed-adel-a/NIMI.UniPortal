// ACCEPTANCE TEST — written by the consultant for phase P09. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Lifecycle;
using static NIMI.UniPortal.Modules.IntegrationTests.Lifecycle.RegistrationSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Grades;

/// <summary>Grading helpers through the real API (P9).</summary>
internal static class GradesSetup
{
    /// <summary>The campus with Ahmed and Mona registered in CS301 group 1 (Hany teaches CS301).</summary>
    public static async Task<CampusWorld> GradedCampusAsync(PortalWebAppFactory factory, HttpClient admin)
    {
        var w = await BuildCampusAsync(factory, admin);
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);
        await RegisteredAsync(w.MonaClient, w.Cs301Group1);
        return w;
    }

    public static async Task<HttpResponseMessage> PutDraftsAsync(HttpClient teacher, Guid groupId, params (Guid StudentId, decimal Score)[] entries) =>
        await teacher.PutAsJsonAsync(
            $"/api/grades/groups/{groupId}",
            new { entries = entries.Select(e => new { studentId = e.StudentId, score = e.Score }).ToArray() });

    public static async Task<JsonObject> DraftedAsync(HttpClient teacher, Guid groupId, params (Guid StudentId, decimal Score)[] entries)
    {
        var response = await PutDraftsAsync(teacher, groupId, entries);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await BodyOf(response);
    }

    public static async Task<JsonObject> GroupGradesAsync(HttpClient client, Guid groupId) =>
        (await client.GetFromJsonAsync<JsonObject>($"/api/grades/groups/{groupId}"))!;

    /// <summary>The roster row of one student in a group-grades body.</summary>
    public static JsonNode Row(JsonObject groupGrades, Guid studentId) =>
        groupGrades["rows"]!.AsArray().Single(r => (Guid)r!["studentId"]! == studentId)!;

    public static async Task<HttpResponseMessage> ConfirmAsync(HttpClient admin, Guid groupId) =>
        await admin.PostAsJsonAsync("/api/admin/grades/confirm", new { groupId });

    public static async Task<int> ConfirmedAsync(HttpClient admin, Guid groupId)
    {
        var response = await ConfirmAsync(admin, groupId);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (int)(await BodyOf(response))["confirmed"]!;
    }

    public static async Task<HttpResponseMessage> PublishAsync(HttpClient admin, Guid? termId, Guid? subjectId = null, Guid? groupId = null) =>
        await admin.PostAsJsonAsync("/api/admin/grades/publish", new { termId, subjectId, groupId });

    public static async Task<int> PublishedAsync(HttpClient admin, Guid termId, Guid? subjectId = null, Guid? groupId = null)
    {
        var response = await PublishAsync(admin, termId, subjectId, groupId);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (int)(await BodyOf(response))["published"]!;
    }

    public static async Task<HttpResponseMessage> CorrectAsync(HttpClient admin, Guid gradeId, decimal score, string? reason) =>
        await admin.PostAsJsonAsync($"/api/admin/grades/{gradeId}/correct", new { score, reason });

    public static async Task<JsonObject> MyGradesAsync(HttpClient student, Guid? termId = null) =>
        (await student.GetFromJsonAsync<JsonObject>(termId is null ? "/api/grades/mine" : $"/api/grades/mine?termId={termId}"))!;

    /// <summary>Drafts, confirms and publishes one group's scores in one go.</summary>
    public static async Task GradeAndPublishAsync(
        HttpClient teacher, HttpClient admin, Guid termId, Guid groupId, params (Guid StudentId, decimal Score)[] entries)
    {
        await DraftedAsync(teacher, groupId, entries);
        await ConfirmedAsync(admin, groupId);
        await PublishedAsync(admin, termId, groupId: groupId);
    }

    public static async Task<int> UnreadAsync(HttpClient client) =>
        (int)(await client.GetFromJsonAsync<JsonObject>("/api/notifications/mine/unread-count"))!["count"]!;
}
