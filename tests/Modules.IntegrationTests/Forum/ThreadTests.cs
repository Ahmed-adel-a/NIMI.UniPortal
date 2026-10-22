// ACCEPTANCE TEST — written by the consultant for phase P11. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Forum.ForumSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Forum;

/// <summary>P11 Forum — threads carry the author's identity (D4: name, rank, department — never anonymous).</summary>
public sealed class ThreadTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task A_thread_shows_who_wrote_it()
    {
        var f = await BuildForumAsync(Factory, Admin());

        var response = await StartThreadAsync(f.Campus.AhmedClient, f.General, "Study group for CS301", "Who is in?");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var thread = await BodyOf(response);
        Assert.Equal("Study group for CS301", (string?)thread["title"]);
        Assert.Equal("Who is in?", (string?)thread["body"]);
        Assert.Equal(f.General, (Guid)thread["categoryId"]!);
        var author = thread["author"]!;
        Assert.Equal(f.Campus.Ahmed, (Guid)author["id"]!);
        Assert.Equal("Test Student", (string?)author["name"]);
        Assert.Equal("student", (string?)author["rank"]);
        Assert.Equal("CS", (string?)author["department"]);
        Assert.Equal(0, (int)thread["replyCount"]!);
        Assert.False((bool)thread["isLocked"]!);
    }

    [Fact]
    public async Task A_professors_rank_is_their_academic_title()
    {
        var f = await BuildForumAsync(Factory, Admin());

        var thread = await BodyOf(await StartThreadAsync(f.Campus.HanyClient, f.General, "Office hours"));

        Assert.Equal("professor", (string?)thread["author"]!["rank"]);
    }

    [Fact]
    public async Task Threads_are_listed_pinned_first_then_by_latest_activity()
    {
        var f = await BuildForumAsync(Factory, Admin());
        var older = await StartedThreadAsync(f.Campus.AhmedClient, f.General, "Older");
        var newer = await StartedThreadAsync(f.Campus.MonaClient, f.General, "Newer");
        Factory.Clock.Advance(TimeSpan.FromMinutes(2));
        await RepliedAsync(f.Campus.MonaClient, older);                        // older becomes the most active
        var pinned = await StartedThreadAsync(f.Campus.AhmedClient, f.General, "Rules");
        Assert.Equal(HttpStatusCode.OK, (await Admin().PostAsJsonAsync($"/api/forum/threads/{pinned}/pin", new { pinned = true })).StatusCode);
        Factory.Clock.Advance(TimeSpan.FromMinutes(2));
        await RepliedAsync(f.Campus.MonaClient, newer);

        var list = await ThreadsAsync(f.Campus.AhmedClient, f.General);

        Assert.Equal(3, (int)list["totalCount"]!);
        Assert.Equal([pinned, newer, older], list["items"]!.AsArray().Select(t => (Guid)t!["id"]!));
    }

    [Fact]
    public async Task A_thread_can_be_linked_to_a_known_subject()
    {
        var f = await BuildForumAsync(Factory, Admin());

        var linked = await BodyOf(await StartThreadAsync(f.Campus.AhmedClient, f.Engineering, "CS301 quiz", subjectId: f.Campus.Cs301));
        var unknown = await StartThreadAsync(f.Campus.AhmedClient, f.Engineering, "Ghost", subjectId: Guid.CreateVersion7());

        Assert.Equal(f.Campus.Cs301, (Guid)linked["subjectId"]!);
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Contains("subjectId", await FieldsOf(unknown));
    }

    [Fact]
    public async Task Another_facultys_category_is_invisible()
    {
        var f = await BuildForumAsync(Factory, Admin());
        var thread = await StartedThreadAsync(f.Campus.AhmedClient, f.Engineering);

        Assert.Equal(HttpStatusCode.NotFound, (await f.SaraClient.GetAsync($"/api/forum/categories/{f.Engineering}/threads")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await StartThreadAsync(f.SaraClient, f.Engineering)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await f.SaraClient.GetAsync($"/api/forum/threads/{thread}")).StatusCode);
    }

    [Fact]
    public async Task Threads_are_validated()
    {
        var f = await BuildForumAsync(Factory, Admin());

        var noTitle = await StartThreadAsync(f.Campus.AhmedClient, f.General, " ");
        Assert.Equal(HttpStatusCode.BadRequest, noTitle.StatusCode);
        Assert.Contains("title", await FieldsOf(noTitle));
        var longBody = await StartThreadAsync(f.Campus.AhmedClient, f.General, "Long", new string('x', 10_001));
        Assert.Equal(HttpStatusCode.BadRequest, longBody.StatusCode);
        Assert.Contains("body", await FieldsOf(longBody));
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");
}
