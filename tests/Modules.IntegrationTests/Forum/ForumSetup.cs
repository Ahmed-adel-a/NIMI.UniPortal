// ACCEPTANCE TEST — written by the consultant for phase P11. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Lifecycle;
using NIMI.UniPortal.Modules.IntegrationTests.Users;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Forum;

/// <summary>Forum helpers through the real API (P11, decision D4: identity required).</summary>
internal static class ForumSetup
{
    /// <summary>
    /// The campus (ENG faculty; Ahmed, Mona = CS students; Hany = CS professor teaching CS301) plus a university-wide
    /// category "General" and an ENG-only category "Engineering". Sara is a student of another faculty (COM).
    /// </summary>
    public static async Task<ForumWorld> BuildForumAsync(PortalWebAppFactory factory, HttpClient admin)
    {
        var campus = await RegistrationSetup.BuildCampusAsync(factory, admin);
        var general = await CreatedCategoryAsync(admin, "General", null);
        var engineering = await CreatedCategoryAsync(admin, "Engineering", campus.FacultyId);
        var com = await OrganizationSetup.CreateFacultyAsync(admin, "COM");
        var acc = await OrganizationSetup.CreateDepartmentAsync(admin, com, "ACC");
        var (sara, saraToken) = await AccountSetup.CreateStudentWithTokenAsync(factory, admin, acc, "sara@cu.edu.eg");
        return new ForumWorld(campus, general, engineering, sara, factory.CreateClientWithToken(saraToken));
    }

    public static async Task<HttpResponseMessage> CreateCategoryAsync(HttpClient admin, string name, Guid? facultyId, int sortOrder = 0) =>
        await admin.PostAsJsonAsync("/api/admin/forum/categories", new { name, nameAr = name + " (ar)", facultyId, sortOrder });

    public static async Task<Guid> CreatedCategoryAsync(HttpClient admin, string name, Guid? facultyId, int sortOrder = 0)
    {
        var response = await CreateCategoryAsync(admin, name, facultyId, sortOrder);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (Guid)(await BodyOf(response))["id"]!;
    }

    public static async Task<HttpResponseMessage> StartThreadAsync(
        HttpClient client, Guid categoryId, string title = "Dijkstra with negative edges?", string body = "Does it still work?", Guid? subjectId = null) =>
        await client.PostAsJsonAsync($"/api/forum/categories/{categoryId}/threads", new { title, body, subjectId });

    public static async Task<Guid> StartedThreadAsync(
        HttpClient client, Guid categoryId, string title = "Dijkstra with negative edges?", Guid? subjectId = null)
    {
        var response = await StartThreadAsync(client, categoryId, title, subjectId: subjectId);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (Guid)(await BodyOf(response))["id"]!;
    }

    public static async Task<HttpResponseMessage> ReplyAsync(HttpClient client, Guid threadId, string body = "No — use Bellman-Ford.") =>
        await client.PostAsJsonAsync($"/api/forum/threads/{threadId}/posts", new { body });

    public static async Task<Guid> RepliedAsync(HttpClient client, Guid threadId, string body = "No — use Bellman-Ford.")
    {
        var response = await ReplyAsync(client, threadId, body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (Guid)(await BodyOf(response))["id"]!;
    }

    public static async Task<JsonObject> ThreadAsync(HttpClient client, Guid threadId) =>
        (await client.GetFromJsonAsync<JsonObject>($"/api/forum/threads/{threadId}"))!;

    public static async Task<JsonObject> ThreadsAsync(HttpClient client, Guid categoryId) =>
        (await client.GetFromJsonAsync<JsonObject>($"/api/forum/categories/{categoryId}/threads"))!;

    public static JsonNode Post(JsonObject thread, Guid postId) =>
        thread["posts"]!["items"]!.AsArray().Single(p => (Guid)p!["id"]! == postId)!;

    public static async Task<int> UnreadAsync(HttpClient client) =>
        (int)(await client.GetFromJsonAsync<JsonObject>("/api/notifications/mine/unread-count"))!["count"]!;
}
