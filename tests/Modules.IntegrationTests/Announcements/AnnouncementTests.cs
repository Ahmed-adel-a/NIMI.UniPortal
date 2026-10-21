// ACCEPTANCE TEST — written by the consultant for phase P07-announcements. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Lifecycle;
using static NIMI.UniPortal.Modules.IntegrationTests.Lifecycle.RegistrationSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Announcements;

/// <summary>
/// P7.4–P7.5 — subject announcements by teaching staff (ANN), the student feed, expiry and urgent notifications (R5, R6).
/// Campus: <see cref="CampusWorld"/>. Hany teaches CS301 (and CS303 where a test assigns it).
/// </summary>
public sealed class AnnouncementTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task The_teaching_professor_posts_an_announcement()
    {
        var w = await Campus();

        var response = await PostAsync(w.HanyClient, w.Cs301, "Midterm moved", body: "الامتحان يوم الأحد في القاعة ٣٠٢", priority: "high", isPinned: true);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var announcement = await BodyOf(response);
        Assert.Equal(w.Cs301, (Guid)announcement["subjectId"]!);
        Assert.Equal("CS301", (string?)announcement["subjectCode"]);
        Assert.Null(announcement["subjectGroupId"]);
        Assert.Equal("Midterm moved", (string?)announcement["title"]);
        Assert.Equal("الامتحان يوم الأحد في القاعة ٣٠٢", (string?)announcement["body"]);
        Assert.Equal("high", (string?)announcement["priority"]);
        Assert.True((bool)announcement["isPinned"]!);
        Assert.Null(announcement["expiresAt"]);
        Assert.Equal(w.Hany, (Guid)announcement["authorId"]!);
        Assert.Equal("Test Professor", (string?)announcement["authorName"]);
    }

    [Fact]
    public async Task Only_teaching_staff_post_and_the_content_must_be_valid()
    {
        var w = await Campus();
        var (_, otherToken) = await AccountSetup.CreateProfessorWithTokenAsync(Factory, Admin(), w.CsDepartmentId, "laila@cu.edu.eg");

        var otherProfessor = await PostAsync(Factory.CreateClientWithToken(otherToken), w.Cs301, "Hello");
        var student = await PostAsync(w.AhmedClient, w.Cs301, "Hello");
        var wrongGroup = await PostAsync(w.HanyClient, w.Cs301, "Hello", subjectGroupId: w.Cs302Group1);
        var pastExpiry = await PostAsync(w.HanyClient, w.Cs301, "Hello", expiresAt: Factory.Clock.GetUtcNow().AddHours(-1));
        var noTitle = await PostAsync(w.HanyClient, w.Cs301, "");

        Assert.Equal(HttpStatusCode.Forbidden, otherProfessor.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, student.StatusCode);
        Assert.Contains("subjectGroupId", await FieldsOf(wrongGroup));
        Assert.Contains("expiresAt", await FieldsOf(pastExpiry));
        Assert.Contains("title", await FieldsOf(noTitle));
    }

    [Fact]
    public async Task Registered_students_see_pinned_first_then_newest_and_only_their_groups_items()
    {
        var w = await Campus();
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);
        await RegisteredAsync(w.MonaClient, w.Cs301Group2);
        await PostedAsync(w, w.Cs301, "Old pinned", isPinned: true);
        Factory.Clock.Advance(TimeSpan.FromMinutes(1));
        await PostedAsync(w, w.Cs301, "For group 2 only", subjectGroupId: w.Cs301Group2);
        Factory.Clock.Advance(TimeSpan.FromMinutes(1));
        await PostedAsync(w, w.Cs301, "Newest");

        var ahmed = await ListAsync(w.AhmedClient, w.Cs301);
        var mona = await ListAsync(w.MonaClient, w.Cs301);

        Assert.Equal(["Old pinned", "Newest"], Titles(ahmed));
        Assert.Equal(["Old pinned", "Newest", "For group 2 only"], Titles(mona));
    }

    [Fact]
    public async Task Students_not_registered_in_the_subject_are_refused()
    {
        var w = await Campus();
        await PostedAsync(w, w.Cs301, "Hello");

        Assert.Equal(HttpStatusCode.Forbidden, (await w.AhmedClient.GetAsync($"/api/announcements?subjectId={w.Cs301}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await w.HanyClient.GetAsync("/api/announcements")).StatusCode);   // subject required
    }

    [Fact]
    public async Task Expired_announcements_are_hidden_from_students_but_not_from_teachers()
    {
        var w = await Campus();
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);
        await PostedAsync(w, w.Cs301, "Office hours today", expiresAt: Factory.Clock.GetUtcNow().AddHours(1));
        await PostedAsync(w, w.Cs301, "Syllabus");

        Factory.Clock.Advance(TimeSpan.FromHours(2));

        Assert.Equal(["Syllabus"], Titles(await ListAsync(w.AhmedClient, w.Cs301)));
        Assert.Equal(["Syllabus"], Titles(await FeedAsync(w.AhmedClient)));
        Assert.Equal(2, (int)(await ListAsync(w.HanyClient, w.Cs301))["totalCount"]!);
        Assert.Equal(2, (int)(await ListAsync(Admin(), w.Cs301))["totalCount"]!);
    }

    [Fact]
    public async Task The_feed_covers_every_registered_subject_pinned_first()
    {
        var w = await Campus();
        await AssignTeacherAsync(Admin(), w.Hany, w.Cs303, w.TermId);
        await AssignTeacherAsync(Admin(), w.Hany, w.Cs302, w.TermId);
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);
        await RegisteredAsync(w.AhmedClient, w.Cs303Group1);
        await PostedAsync(w, w.Cs303, "CS303 pinned", isPinned: true);
        Factory.Clock.Advance(TimeSpan.FromMinutes(1));
        await PostedAsync(w, w.Cs302, "CS302 news");                               // Ahmed is not registered in CS302
        Factory.Clock.Advance(TimeSpan.FromMinutes(1));
        await PostedAsync(w, w.Cs301, "CS301 news");

        var feed = await FeedAsync(w.AhmedClient);

        Assert.Equal(["CS303 pinned", "CS301 news"], Titles(feed));
        Assert.Equal(["CS303", "CS301"], feed["items"]!.AsArray().Select(a => (string?)a!["subjectCode"]));
        Assert.Equal(HttpStatusCode.Forbidden, (await w.HanyClient.GetAsync("/api/announcements/feed")).StatusCode);
    }

    [Fact]
    public async Task Only_urgent_announcements_notify_the_targeted_students()
    {
        var w = await Campus();
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);
        await RegisteredAsync(w.MonaClient, w.Cs301Group2);

        await PostedAsync(w, w.Cs301, "Reading list", priority: "normal");
        await PostedAsync(w, w.Cs301, "Lab cancelled today", priority: "urgent", subjectGroupId: w.Cs301Group1);

        Assert.Equal(1, await UnreadAsync(w.AhmedClient));
        Assert.Equal(0, await UnreadAsync(w.MonaClient));
    }

    [Fact]
    public async Task The_author_edits_and_deletes_and_students_cannot()
    {
        var w = await Campus();
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);
        var id = await PostedAsync(w, w.Cs301, "Midterm moved");
        var body = new { title = "Midterm moved to Monday", body = "Hall 302", priority = "high", isPinned = false, expiresAt = (DateTimeOffset?)null };

        var edit = await w.HanyClient.PutAsJsonAsync($"/api/announcements/{id}", body);
        var studentEdit = await w.AhmedClient.PutAsJsonAsync($"/api/announcements/{id}", body);
        var delete = await w.HanyClient.DeleteAsync($"/api/announcements/{id}");

        Assert.Equal(HttpStatusCode.OK, edit.StatusCode);
        Assert.Equal("Midterm moved to Monday", (string?)(await BodyOf(edit))["title"]);
        Assert.Equal(HttpStatusCode.Forbidden, studentEdit.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(0, (int)(await ListAsync(w.AhmedClient, w.Cs301))["totalCount"]!);
        Assert.Equal(HttpStatusCode.NotFound, (await w.HanyClient.DeleteAsync($"/api/announcements/{id}")).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private Task<CampusWorld> Campus() => BuildCampusAsync(Factory, Admin());

    private static async Task<HttpResponseMessage> PostAsync(
        HttpClient client, Guid subjectId, string title, string body = "Details in the attached sheet.", string priority = "normal",
        bool isPinned = false, Guid? subjectGroupId = null, DateTimeOffset? expiresAt = null) =>
        await client.PostAsJsonAsync("/api/announcements", new { subjectId, subjectGroupId, title, body, priority, isPinned, expiresAt });

    private static async Task<Guid> PostedAsync(
        CampusWorld w, Guid subjectId, string title, string priority = "normal", bool isPinned = false,
        Guid? subjectGroupId = null, DateTimeOffset? expiresAt = null)
    {
        var response = await PostAsync(w.HanyClient, subjectId, title, priority: priority, isPinned: isPinned,
            subjectGroupId: subjectGroupId, expiresAt: expiresAt);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (Guid)(await BodyOf(response))["id"]!;
    }

    private static async Task<JsonObject> ListAsync(HttpClient client, Guid subjectId) =>
        (await client.GetFromJsonAsync<JsonObject>($"/api/announcements?subjectId={subjectId}"))!;

    private static async Task<JsonObject> FeedAsync(HttpClient student) =>
        (await student.GetFromJsonAsync<JsonObject>("/api/announcements/feed"))!;

    private static List<string?> Titles(JsonObject page) => page["items"]!.AsArray().Select(a => (string?)a!["title"]).ToList();

    private static async Task<int> UnreadAsync(HttpClient client) =>
        (int)(await client.GetFromJsonAsync<JsonObject>("/api/notifications/mine/unread-count"))!["count"]!;
}
