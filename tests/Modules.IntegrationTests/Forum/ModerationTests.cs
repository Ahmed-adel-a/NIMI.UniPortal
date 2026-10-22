// ACCEPTANCE TEST — written by the consultant for phase P11. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Forum.ForumSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Forum;

/// <summary>P11 Forum — pin/lock (Student Affairs, or the subject's teacher on a subject thread) and reports.</summary>
public sealed class ModerationTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task Student_affairs_pins_and_locks_any_thread()
    {
        var f = await BuildForumAsync(Factory, Admin());
        var threadId = await StartedThreadAsync(f.Campus.AhmedClient, f.General);

        var pinned = await Admin().PostAsJsonAsync($"/api/forum/threads/{threadId}/pin", new { pinned = true });
        var locked = await Admin().PostAsJsonAsync($"/api/forum/threads/{threadId}/lock", new { locked = true });

        Assert.True((bool)(await BodyOf(pinned))["isPinned"]!);
        Assert.True((bool)(await BodyOf(locked))["isLocked"]!);
        var unlocked = await Admin().PostAsJsonAsync($"/api/forum/threads/{threadId}/lock", new { locked = false });
        Assert.False((bool)(await BodyOf(unlocked))["isLocked"]!);
    }

    [Fact]
    public async Task The_subjects_teacher_moderates_only_threads_linked_to_that_subject()
    {
        var f = await BuildForumAsync(Factory, Admin());
        var cs301Thread = await StartedThreadAsync(f.Campus.AhmedClient, f.Engineering, "CS301 quiz", f.Campus.Cs301);
        var cs302Thread = await StartedThreadAsync(f.Campus.AhmedClient, f.Engineering, "CS302 quiz", f.Campus.Cs302);
        var general = await StartedThreadAsync(f.Campus.AhmedClient, f.General, "Anything");

        Assert.Equal(HttpStatusCode.OK, (await f.Campus.HanyClient.PostAsJsonAsync($"/api/forum/threads/{cs301Thread}/lock", new { locked = true })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Campus.HanyClient.PostAsJsonAsync($"/api/forum/threads/{cs302Thread}/lock", new { locked = true })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Campus.HanyClient.PostAsJsonAsync($"/api/forum/threads/{general}/pin", new { pinned = true })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Campus.AhmedClient.PostAsJsonAsync($"/api/forum/threads/{general}/pin", new { pinned = true })).StatusCode);
    }

    [Fact]
    public async Task A_reported_post_can_be_removed()
    {
        var f = await BuildForumAsync(Factory, Admin());
        var threadId = await StartedThreadAsync(f.Campus.AhmedClient, f.General);
        var postId = await RepliedAsync(f.Campus.MonaClient, threadId, "spam spam spam");

        var reported = await f.Campus.AhmedClient.PostAsJsonAsync("/api/forum/reports", new { postId, reason = "Spam" });
        Assert.Equal(HttpStatusCode.Created, reported.StatusCode);
        var reportId = (Guid)(await BodyOf(reported))["id"]!;

        var open = (await Admin().GetFromJsonAsync<JsonObject>("/api/admin/forum/reports?status=open"))!;
        var item = Assert.Single(open["items"]!.AsArray())!;
        Assert.Equal(postId, (Guid)item["postId"]!);
        Assert.Equal(f.Campus.Ahmed, (Guid)item["reportedBy"]!);
        Assert.Equal("Spam", (string?)item["reason"]);

        var resolved = await Admin().PostAsJsonAsync($"/api/admin/forum/reports/{reportId}/resolve", new { action = "remove" });
        Assert.Equal(HttpStatusCode.OK, resolved.StatusCode);
        Assert.Equal("resolved", (string?)(await BodyOf(resolved))["status"]);
        Assert.True((bool)Post(await ThreadAsync(f.Campus.AhmedClient, threadId), postId)["isDeleted"]!);
        Assert.Equal(0, (int)(await Admin().GetFromJsonAsync<JsonObject>("/api/admin/forum/reports?status=open"))!["totalCount"]!);
        Assert.Equal(HttpStatusCode.Conflict, (await Admin().PostAsJsonAsync($"/api/admin/forum/reports/{reportId}/resolve", new { action = "dismiss" })).StatusCode);
    }

    [Fact]
    public async Task A_dismissed_report_leaves_the_thread()
    {
        var f = await BuildForumAsync(Factory, Admin());
        var threadId = await StartedThreadAsync(f.Campus.AhmedClient, f.General);
        var reported = await f.Campus.MonaClient.PostAsJsonAsync("/api/forum/reports", new { threadId, reason = "I disagree" });
        var reportId = (Guid)(await BodyOf(reported))["id"]!;

        Assert.Equal(HttpStatusCode.OK, (await Admin().PostAsJsonAsync($"/api/admin/forum/reports/{reportId}/resolve", new { action = "dismiss" })).StatusCode);

        Assert.False((bool)(await ThreadAsync(f.Campus.MonaClient, threadId))["isDeleted"]!);
    }

    [Fact]
    public async Task A_report_names_exactly_one_thread_or_post_and_a_reason()
    {
        var f = await BuildForumAsync(Factory, Admin());
        var threadId = await StartedThreadAsync(f.Campus.AhmedClient, f.General);
        var postId = await RepliedAsync(f.Campus.MonaClient, threadId);

        Assert.Equal(HttpStatusCode.BadRequest, (await f.Campus.AhmedClient.PostAsJsonAsync("/api/forum/reports", new { reason = "?" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await f.Campus.AhmedClient.PostAsJsonAsync("/api/forum/reports", new { threadId, postId, reason = "both" })).StatusCode);
        var noReason = await f.Campus.AhmedClient.PostAsJsonAsync("/api/forum/reports", new { postId, reason = " " });
        Assert.Equal(HttpStatusCode.BadRequest, noReason.StatusCode);
        Assert.Contains("reason", await FieldsOf(noReason));
        Assert.Equal(HttpStatusCode.NotFound, (await f.Campus.AhmedClient.PostAsJsonAsync("/api/forum/reports", new { postId = Guid.CreateVersion7(), reason = "ghost" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Campus.HanyClient.GetAsync("/api/admin/forum/reports")).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");
}
