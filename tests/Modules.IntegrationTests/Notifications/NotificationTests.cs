// ACCEPTANCE TEST — written by the consultant for phase P02. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Users;
using NIMI.UniPortal.Modules.Notifications.Contracts;

namespace NIMI.UniPortal.Modules.IntegrationTests.Notifications;

/// <summary>P2.1 + P2.2 — NotificationRequested → stored per recipient → read API for the signed-in user.</summary>
public sealed class NotificationTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    private const string Mine = "/api/notifications/mine";
    private const string UnreadCount = "/api/notifications/mine/unread-count";
    private const string ReadAll = "/api/notifications/read-all";

    [Fact]
    public async Task A_requested_notification_reaches_every_recipient_once()
    {
        var (ahmed, ahmedClient) = await Student("ahmed@cu.edu.eg");
        var (mona, monaClient) = await Student("mona@cu.edu.eg");

        await Factory.PublishAsync(Request([ahmed.Id, mona.Id, ahmed.Id], title: "Timetable changed"));   // duplicate id on purpose

        var ahmedList = await ListAsync(ahmedClient);
        var monaList = await ListAsync(monaClient);
        Assert.Equal(1, (int)ahmedList["totalCount"]!);
        Assert.Equal(1, (int)monaList["totalCount"]!);
        var item = ahmedList["items"]![0]!;
        Assert.Equal("Timetable changed", (string?)item["title"]);
        Assert.Equal("schedule", (string?)item["type"]);
        Assert.Equal("/schedule", (string?)item["actionUrl"]);
        Assert.False((bool)item["isRead"]!);
        Assert.Null(item["readAt"]);
        Assert.NotNull((string?)item["createdAt"]);
    }

    [Fact]
    public async Task Redelivering_the_same_event_creates_no_duplicates()
    {
        var (ahmed, client) = await Student("ahmed@cu.edu.eg");
        var request = Request([ahmed.Id]);

        await Factory.PublishAsync(request);
        await Factory.PublishAsync(request);

        Assert.Equal(1, (int)(await ListAsync(client))["totalCount"]!);
    }

    [Fact]
    public async Task An_invalid_event_is_dropped_without_failing_and_without_storing_anything()
    {
        var (ahmed, client) = await Student("ahmed@cu.edu.eg");

        var unknownType = await Record.ExceptionAsync(() => Factory.PublishAsync(Request([ahmed.Id], type: "party")));
        var emptyTitle = await Record.ExceptionAsync(() => Factory.PublishAsync(Request([ahmed.Id], title: "")));

        Assert.Null(unknownType);
        Assert.Null(emptyTitle);
        Assert.Equal(0, (int)(await ListAsync(client))["totalCount"]!);
    }

    [Fact]
    public async Task My_notifications_are_newest_first_paged_and_filterable_by_unread()
    {
        var (ahmed, client) = await Student("ahmed@cu.edu.eg");
        foreach (var title in new[] { "first", "second", "third" })
        {
            await Factory.PublishAsync(Request([ahmed.Id], title: title));
            Factory.Clock.Advance(TimeSpan.FromMinutes(1));
        }
        var oldest = (await ListAsync(client, "?page=2&pageSize=2"))["items"]![0]!;
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"/api/notifications/{(Guid)oldest["id"]!}/read", null)).StatusCode);

        var firstPage = await ListAsync(client, "?page=1&pageSize=2");
        var unreadOnly = await ListAsync(client, "?unreadOnly=true");

        Assert.Equal("first", (string?)oldest["title"]);
        Assert.Equal(3, (int)firstPage["totalCount"]!);
        Assert.Equal(2, (int)firstPage["totalPages"]!);
        Assert.Equal(1, (int)firstPage["page"]!);
        Assert.Equal(2, (int)firstPage["pageSize"]!);
        Assert.Equal(["third", "second"], firstPage["items"]!.AsArray().Select(i => (string?)i!["title"]));
        Assert.Equal(2, (int)unreadOnly["totalCount"]!);
        Assert.DoesNotContain(unreadOnly["items"]!.AsArray(), i => (string?)i!["title"] == "first");
    }

    [Fact]
    public async Task Page_size_is_capped_at_100_like_every_list()
    {
        var (_, client) = await Student("ahmed@cu.edu.eg");

        Assert.Equal(100, (int)(await ListAsync(client, "?pageSize=500"))["pageSize"]!);
    }

    [Fact]
    public async Task Marking_one_read_updates_the_unread_count_and_keeps_the_first_read_time()
    {
        var (ahmed, client) = await Student("ahmed@cu.edu.eg");
        await Factory.PublishAsync(Request([ahmed.Id], title: "one"));
        Factory.Clock.Advance(TimeSpan.FromMinutes(1));
        await Factory.PublishAsync(Request([ahmed.Id], title: "two"));                           // newest → always items[0]
        Assert.Equal(2, await CountAsync(client));
        var id = (Guid)(await ListAsync(client))["items"]![0]!["id"]!;

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"/api/notifications/{id}/read", null)).StatusCode);
        var firstReadAt = (string?)(await ListAsync(client))["items"]![0]!["readAt"];
        Factory.Clock.Advance(TimeSpan.FromHours(1));
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"/api/notifications/{id}/read", null)).StatusCode);

        Assert.Equal(1, await CountAsync(client));
        var item = (await ListAsync(client))["items"]![0]!;
        Assert.True((bool)item["isRead"]!);
        Assert.NotNull(firstReadAt);
        Assert.Equal(firstReadAt, (string?)item["readAt"]);
    }

    [Fact]
    public async Task I_cannot_mark_someone_elses_notification()
    {
        var (ahmed, ahmedClient) = await Student("ahmed@cu.edu.eg");
        var (_, monaClient) = await Student("mona@cu.edu.eg");
        await Factory.PublishAsync(Request([ahmed.Id]));
        var ahmedsId = (Guid)(await ListAsync(ahmedClient))["items"]![0]!["id"]!;

        var someoneElses = await monaClient.PostAsync($"/api/notifications/{ahmedsId}/read", null);
        var unknown = await monaClient.PostAsync($"/api/notifications/{Guid.CreateVersion7()}/read", null);

        Assert.Equal(HttpStatusCode.NotFound, someoneElses.StatusCode);
        Assert.Equal("not_found", (string?)(await someoneElses.Content.ReadFromJsonAsync<JsonObject>())!["code"]);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(1, await CountAsync(ahmedClient));                                          // still unread for Ahmed
    }

    [Fact]
    public async Task Read_all_marks_everything_of_mine_and_nothing_of_others()
    {
        var (ahmed, ahmedClient) = await Student("ahmed@cu.edu.eg");
        var (mona, monaClient) = await Student("mona@cu.edu.eg");
        await Factory.PublishAsync(Request([ahmed.Id, mona.Id], title: "one"));
        await Factory.PublishAsync(Request([ahmed.Id, mona.Id], title: "two"));

        var response = await ahmedClient.PostAsync(ReadAll, null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(0, await CountAsync(ahmedClient));
        Assert.Equal(2, await CountAsync(monaClient));
        Assert.Equal(HttpStatusCode.NoContent, (await ahmedClient.PostAsync(ReadAll, null)).StatusCode);   // nothing left: still 204
    }

    [Fact]
    public async Task Professors_get_their_notifications_too()
    {
        var (hany, client) = await Professor("hany@cu.edu.eg");

        await Factory.PublishAsync(Request([hany.Id], type: "system", title: "Welcome"));

        Assert.Equal(1, await CountAsync(client));
    }

    [Fact]
    public async Task Arabic_texts_round_trip()
    {
        var (ahmed, client) = await Student("ahmed@cu.edu.eg");

        await Factory.PublishAsync(Request([ahmed.Id], titleAr: "تغيير في الجدول", messageAr: "تم نقل محاضرة CS301 إلى قاعة ٣٠٢ يوم الأحد."));

        var item = (await ListAsync(client))["items"]![0]!;
        Assert.Equal("تغيير في الجدول", (string?)item["titleAr"]);
        Assert.Equal("تم نقل محاضرة CS301 إلى قاعة ٣٠٢ يوم الأحد.", (string?)item["messageAr"]);
    }

    [Fact]
    public async Task Anonymous_callers_get_401()
    {
        var client = Factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(Mine)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(UnreadCount)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync($"/api/notifications/{Guid.CreateVersion7()}/read", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync(ReadAll, null)).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private async Task<Guid> Department() =>
        await OrganizationSetup.CreateDepartmentAsync(Admin(), await OrganizationSetup.CreateFacultyAsync(Admin(), "ENG"), "CS");

    private Guid? _department;

    private async Task<((Guid Id, string Token) Account, HttpClient Client)> Student(string email)
    {
        _department ??= await Department();
        var account = await AccountSetup.CreateStudentWithTokenAsync(Factory, Admin(), _department.Value, email);
        return (account, Factory.CreateClientWithToken(account.Token));
    }

    private async Task<((Guid Id, string Token) Account, HttpClient Client)> Professor(string email)
    {
        _department ??= await Department();
        var account = await AccountSetup.CreateProfessorWithTokenAsync(Factory, Admin(), _department.Value, email);
        return (account, Factory.CreateClientWithToken(account.Token));
    }

    private NotificationRequested Request(
        Guid[] recipients,
        string type = "schedule",
        string title = "Timetable changed",
        string titleAr = "تغيير في الجدول",
        string message = "CS301 moved to hall 302 on Sunday.",
        string messageAr = "تم نقل محاضرة CS301 إلى قاعة ٣٠٢.") =>
        new(Guid.CreateVersion7(), Factory.Clock.GetUtcNow(), recipients, type, title, titleAr, message, messageAr, "/schedule");

    private static async Task<JsonObject> ListAsync(HttpClient client, string query = "") =>
        (await client.GetFromJsonAsync<JsonObject>(Mine + query))!;

    private static async Task<int> CountAsync(HttpClient client) =>
        (int)(await client.GetFromJsonAsync<JsonObject>(UnreadCount))!["count"]!;
}
