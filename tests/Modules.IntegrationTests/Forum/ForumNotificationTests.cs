// ACCEPTANCE TEST — written by the consultant for phase P11. Do not modify; report problems instead.
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Forum.ForumSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Forum;

/// <summary>
/// P11 Forum — a reply notifies the thread's author (type <c>forum</c>), at most once until the author opens the thread
/// again; replying to your own thread notifies no one.
/// </summary>
public sealed class ForumNotificationTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task A_reply_notifies_the_threads_author()
    {
        var f = await BuildForumAsync(Factory, Admin());
        var threadId = await StartedThreadAsync(f.Campus.AhmedClient, f.General, "Study group");
        var before = await UnreadAsync(f.Campus.AhmedClient);

        await RepliedAsync(f.Campus.MonaClient, threadId);

        Assert.Equal(before + 1, await UnreadAsync(f.Campus.AhmedClient));
        var latest = (await f.Campus.AhmedClient.GetFromJsonAsync<JsonObject>("/api/notifications/mine?pageSize=1"))!["items"]![0]!;
        Assert.Equal("forum", (string?)latest["type"]);
        Assert.Contains("Study group", (string?)latest["title"]);
    }

    [Fact]
    public async Task Further_replies_wait_until_the_author_has_looked()
    {
        var f = await BuildForumAsync(Factory, Admin());
        var threadId = await StartedThreadAsync(f.Campus.AhmedClient, f.General);
        var before = await UnreadAsync(f.Campus.AhmedClient);

        await RepliedAsync(f.Campus.MonaClient, threadId, "one");
        await RepliedAsync(f.Campus.HanyClient, threadId, "two");
        Assert.Equal(before + 1, await UnreadAsync(f.Campus.AhmedClient));

        await ThreadAsync(f.Campus.AhmedClient, threadId);                     // the author reads the thread
        await RepliedAsync(f.Campus.MonaClient, threadId, "three");
        Assert.Equal(before + 2, await UnreadAsync(f.Campus.AhmedClient));
    }

    [Fact]
    public async Task Replying_to_your_own_thread_notifies_no_one()
    {
        var f = await BuildForumAsync(Factory, Admin());
        var threadId = await StartedThreadAsync(f.Campus.AhmedClient, f.General);
        var before = await UnreadAsync(f.Campus.AhmedClient);

        await RepliedAsync(f.Campus.AhmedClient, threadId, "bump");

        Assert.Equal(before, await UnreadAsync(f.Campus.AhmedClient));
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");
}
