// ACCEPTANCE TEST — written by the consultant for phase P11. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Forum.ForumSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Forum;

/// <summary>P11 Forum — replies, the 15-minute edit window, soft delete with a placeholder, locks and the rate limit.</summary>
public sealed class PostTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task A_reply_carries_its_author_and_bumps_the_thread()
    {
        var f = await BuildForumAsync(Factory, Admin());
        var threadId = await StartedThreadAsync(f.Campus.AhmedClient, f.General);

        var response = await ReplyAsync(f.Campus.HanyClient, threadId, "Negative edges break it.");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var reply = await BodyOf(response);
        Assert.Equal("Negative edges break it.", (string?)reply["body"]);
        Assert.Equal(f.Campus.Hany, (Guid)reply["author"]!["id"]!);
        Assert.Equal("professor", (string?)reply["author"]!["rank"]);
        var thread = await ThreadAsync(f.Campus.AhmedClient, threadId);
        Assert.Equal(1, (int)thread["replyCount"]!);
        Assert.Equal(1, (int)thread["posts"]!["totalCount"]!);
        Assert.Equal((Guid)reply["id"]!, (Guid)thread["posts"]!["items"]![0]!["id"]!);
    }

    [Fact]
    public async Task Authors_edit_within_15_minutes_and_student_affairs_any_time()
    {
        var f = await BuildForumAsync(Factory, Admin());
        var threadId = await StartedThreadAsync(f.Campus.AhmedClient, f.General);
        var postId = await RepliedAsync(f.Campus.MonaClient, threadId, "first draft");

        var early = await f.Campus.MonaClient.PutAsJsonAsync($"/api/forum/posts/{postId}", new { body = "fixed typo" });
        Assert.Equal(HttpStatusCode.OK, early.StatusCode);
        Assert.Equal("fixed typo", (string?)(await BodyOf(early))["body"]);

        Factory.Clock.Advance(TimeSpan.FromMinutes(16));
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Campus.MonaClient.PutAsJsonAsync($"/api/forum/posts/{postId}", new { body = "too late" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Campus.AhmedClient.PutAsJsonAsync($"/api/forum/threads/{threadId}", new { title = "Late", body = "Late" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Admin().PutAsJsonAsync($"/api/forum/posts/{postId}", new { body = "moderated" })).StatusCode);
    }

    [Fact]
    public async Task Nobody_edits_someone_elses_post()
    {
        var f = await BuildForumAsync(Factory, Admin());
        var threadId = await StartedThreadAsync(f.Campus.AhmedClient, f.General);
        var postId = await RepliedAsync(f.Campus.MonaClient, threadId);

        Assert.Equal(HttpStatusCode.Forbidden, (await f.Campus.AhmedClient.PutAsJsonAsync($"/api/forum/posts/{postId}", new { body = "hijack" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Campus.MonaClient.PutAsJsonAsync($"/api/forum/threads/{threadId}", new { title = "x", body = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Campus.AhmedClient.DeleteAsync($"/api/forum/posts/{postId}")).StatusCode);
    }

    [Fact]
    public async Task A_deleted_post_shows_a_placeholder_except_to_student_affairs()
    {
        var f = await BuildForumAsync(Factory, Admin());
        var threadId = await StartedThreadAsync(f.Campus.AhmedClient, f.General);
        var postId = await RepliedAsync(f.Campus.MonaClient, threadId, "regrettable");

        Assert.Equal(HttpStatusCode.NoContent, (await f.Campus.MonaClient.DeleteAsync($"/api/forum/posts/{postId}")).StatusCode);

        var asMember = Post(await ThreadAsync(f.Campus.AhmedClient, threadId), postId);
        Assert.True((bool)asMember["isDeleted"]!);
        Assert.Null(asMember["body"]);
        var asAdmin = Post(await ThreadAsync(Admin(), threadId), postId);
        Assert.Equal("regrettable", (string?)asAdmin["body"]);
    }

    [Fact]
    public async Task A_deleted_thread_is_gone_for_members()
    {
        var f = await BuildForumAsync(Factory, Admin());
        var threadId = await StartedThreadAsync(f.Campus.AhmedClient, f.General);

        Assert.Equal(HttpStatusCode.NoContent, (await f.Campus.AhmedClient.DeleteAsync($"/api/forum/threads/{threadId}")).StatusCode);

        Assert.Equal(0, (int)(await ThreadsAsync(f.Campus.MonaClient, f.General))["totalCount"]!);
        Assert.Equal(HttpStatusCode.NotFound, (await f.Campus.MonaClient.GetAsync($"/api/forum/threads/{threadId}")).StatusCode);
        Assert.True((bool)(await ThreadAsync(Admin(), threadId))["isDeleted"]!);
    }

    [Fact]
    public async Task A_locked_thread_refuses_replies()
    {
        var f = await BuildForumAsync(Factory, Admin());
        var threadId = await StartedThreadAsync(f.Campus.AhmedClient, f.General);
        Assert.Equal(HttpStatusCode.OK, (await Admin().PostAsJsonAsync($"/api/forum/threads/{threadId}/lock", new { locked = true })).StatusCode);

        Assert.Equal(HttpStatusCode.Conflict, (await ReplyAsync(f.Campus.MonaClient, threadId)).StatusCode);
    }

    [Fact]
    public async Task More_than_10_posts_a_minute_is_rate_limited()
    {
        var f = await BuildForumAsync(Factory, Admin());
        var threadId = await StartedThreadAsync(f.Campus.AhmedClient, f.General);   // 1 of 10 for Ahmed

        for (var i = 0; i < 9; i++)
            Assert.Equal(HttpStatusCode.Created, (await ReplyAsync(f.Campus.AhmedClient, threadId, $"reply {i}")).StatusCode);

        var eleventh = await ReplyAsync(f.Campus.AhmedClient, threadId, "one too many");
        Assert.Equal(HttpStatusCode.TooManyRequests, eleventh.StatusCode);
        Assert.Equal("rate_limited", (string?)(await BodyOf(eleventh))["code"]);
        Assert.Equal(HttpStatusCode.Created, (await ReplyAsync(f.Campus.MonaClient, threadId)).StatusCode);   // per user

        Factory.Clock.Advance(TimeSpan.FromSeconds(61));
        Assert.Equal(HttpStatusCode.Created, (await ReplyAsync(f.Campus.AhmedClient, threadId, "later")).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");
}
