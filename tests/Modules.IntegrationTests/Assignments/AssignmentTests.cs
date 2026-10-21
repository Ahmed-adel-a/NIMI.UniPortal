// ACCEPTANCE TEST — written by the consultant for phase P08. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Lifecycle;
using static NIMI.UniPortal.Modules.IntegrationTests.Assignments.AssignmentsSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Lifecycle.RegistrationSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Assignments;

/// <summary>
/// P8.1–P8.2 — assignments set by teaching staff (R1, R2). Campus: <see cref="CampusWorld"/> — Hany teaches CS301; in these
/// tests Ahmed is registered in CS301 group 1 and Mona in CS301 group 2.
/// </summary>
public sealed class AssignmentTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task The_teaching_professor_sets_an_assignment()
    {
        var w = await Campus();
        var due = Factory.Clock.GetUtcNow().AddDays(7);

        var response = await CreateAsync(w.HanyClient, w.Cs301, due, maxGrade: 20m, allowedExtensions: "pdf,zip", maxFileMb: 5, instructions: "نفّذ خوارزمية ديكسترا");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var assignment = await BodyOf(response);
        Assert.Equal(w.Cs301, (Guid)assignment["subjectId"]!);
        Assert.Equal("CS301", (string?)assignment["subjectCode"]);
        Assert.Equal("Lab 2 — Dijkstra", (string?)assignment["title"]);
        Assert.Equal("نفّذ خوارزمية ديكسترا", (string?)assignment["instructions"]);
        Assert.Equal(20m, (decimal)assignment["maxGrade"]!);
        Assert.Equal(["pdf", "zip"], assignment["allowedExtensions"]!.AsArray().Select(e => (string?)e));
        Assert.Equal(5, (int)assignment["maxFileMb"]!);
        Assert.True((bool)assignment["isOpen"]!);
        Assert.Equal(w.Hany, (Guid)assignment["createdBy"]!);
    }

    [Fact]
    public async Task Defaults_are_pdf_docx_zip_and_10_megabytes()
    {
        var w = await Campus();

        var assignment = await BodyOf(await CreateAsync(w.HanyClient, w.Cs301, Factory.Clock.GetUtcNow().AddDays(7)));

        Assert.Equal(["pdf", "docx", "zip"], assignment["allowedExtensions"]!.AsArray().Select(e => (string?)e));
        Assert.Equal(10, (int)assignment["maxFileMb"]!);
    }

    [Fact]
    public async Task Only_staff_teaching_the_subject_set_assignments_and_values_are_checked()
    {
        var w = await Campus();
        var due = Factory.Clock.GetUtcNow().AddDays(7);

        Assert.Equal(HttpStatusCode.Forbidden, (await CreateAsync(w.HanyClient, w.Cs302, due)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await CreateAsync(w.AhmedClient, w.Cs301, due)).StatusCode);
        Assert.Contains("dueAt", await FieldsOf(await CreateAsync(w.HanyClient, w.Cs301, Factory.Clock.GetUtcNow().AddMinutes(-1))));
        Assert.Contains("maxGrade", await FieldsOf(await CreateAsync(w.HanyClient, w.Cs301, due, maxGrade: 0m)));
        Assert.Contains("subjectGroupId", await FieldsOf(await CreateAsync(w.HanyClient, w.Cs301, due, subjectGroupId: w.Cs302Group1)));
    }

    [Fact]
    public async Task Students_see_their_subjects_assignments_with_their_own_status_and_only_their_groups()
    {
        var w = await Campus();
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);
        await RegisteredAsync(w.MonaClient, w.Cs301Group2);
        var due = Factory.Clock.GetUtcNow().AddDays(7);
        var forAll = await CreatedAsync(w.HanyClient, w.Cs301, due.AddDays(1), "Lab 3");
        var forGroupTwo = await CreatedAsync(w.HanyClient, w.Cs301, due, "Group 2 quiz", w.Cs301Group2);
        await SubmittedAsync(w.AhmedClient, forAll);

        var ahmed = await ListAsync(w.AhmedClient);
        var mona = await ListAsync(w.MonaClient, $"?subjectId={w.Cs301}");

        Assert.Equal(["Lab 3"], Titles(ahmed));
        Assert.Equal("submitted", (string?)ahmed["items"]![0]!["mySubmissionStatus"]);
        Assert.True((long)ahmed["items"]![0]!["dueInSeconds"]! > 0);
        Assert.Null(ahmed["items"]![0]!["submissionCount"]);
        Assert.Equal(["Group 2 quiz", "Lab 3"], Titles(mona));                      // ordered by due date
        Assert.Null(mona["items"]![1]!["mySubmissionStatus"]);
        Assert.Equal(HttpStatusCode.NotFound, (await w.AhmedClient.GetAsync($"/api/assignments/{forGroupTwo}")).StatusCode);
    }

    [Fact]
    public async Task Teachers_see_submission_counts_and_can_filter_open_or_closed()
    {
        var w = await Campus();
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);
        var soon = await CreatedAsync(w.HanyClient, w.Cs301, Factory.Clock.GetUtcNow().AddHours(1), "Quick quiz");
        await CreatedAsync(w.HanyClient, w.Cs301, Factory.Clock.GetUtcNow().AddDays(7), "Lab 3");
        await SubmittedAsync(w.AhmedClient, soon);

        Factory.Clock.Advance(TimeSpan.FromHours(2));
        var open = await ListAsync(w.HanyClient, "?status=open");
        var closed = await ListAsync(w.HanyClient, "?status=closed");

        Assert.Equal(["Lab 3"], Titles(open));
        Assert.Equal(["Quick quiz"], Titles(closed));
        Assert.Equal(1, (int)closed["items"]![0]!["submissionCount"]!);
        Assert.False((bool)closed["items"]![0]!["isOpen"]!);
        Assert.Equal(0, (long)closed["items"]![0]!["dueInSeconds"]!);
    }

    [Fact]
    public async Task Students_not_registered_in_the_subject_cannot_see_its_assignments()
    {
        var w = await Campus();
        var id = await CreatedAsync(w.HanyClient, w.Cs301, Factory.Clock.GetUtcNow().AddDays(7));

        Assert.Equal(HttpStatusCode.Forbidden, (await w.AhmedClient.GetAsync($"/api/assignments?subjectId={w.Cs301}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await w.AhmedClient.GetAsync($"/api/assignments/{id}")).StatusCode);
        Assert.Equal(0, (int)(await ListAsync(w.AhmedClient))["totalCount"]!);       // no registered subjects → nothing
    }

    [Fact]
    public async Task Editing_is_limited_once_students_have_submitted()
    {
        var w = await Campus();
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);
        var due = Factory.Clock.GetUtcNow().AddDays(7);
        var id = await CreatedAsync(w.HanyClient, w.Cs301, due);
        var regraded = new { title = "Lab 2", instructions = (string?)null, dueAt = due.AddDays(2), maxGrade = 50m, allowedExtensions = "pdf,docx,zip", maxFileMb = 10 };
        var extended = new { title = "Lab 2 — clarified", instructions = (string?)"See FAQ", dueAt = due.AddDays(3), maxGrade = 50m, allowedExtensions = "pdf,docx,zip", maxFileMb = 10 };
        var regradedAgain = new { title = "Lab 2", instructions = (string?)null, dueAt = due.AddDays(3), maxGrade = 60m, allowedExtensions = "pdf,docx,zip", maxFileMb = 10 };

        var beforeSubmission = await w.HanyClient.PutAsJsonAsync($"/api/assignments/{id}", regraded);
        await SubmittedAsync(w.AhmedClient, id);
        var extension = await w.HanyClient.PutAsJsonAsync($"/api/assignments/{id}", extended);
        var regradeAfter = await w.HanyClient.PutAsJsonAsync($"/api/assignments/{id}", regradedAgain);
        var studentEdit = await w.AhmedClient.PutAsJsonAsync($"/api/assignments/{id}", extended);

        Assert.Equal(HttpStatusCode.OK, beforeSubmission.StatusCode);
        Assert.Equal(50m, (decimal)(await BodyOf(beforeSubmission))["maxGrade"]!);
        Assert.Equal(HttpStatusCode.OK, extension.StatusCode);
        Assert.Equal("Lab 2 — clarified", (string?)(await BodyOf(extension))["title"]);
        Assert.Equal(HttpStatusCode.BadRequest, regradeAfter.StatusCode);
        Assert.Contains("maxGrade", await FieldsOf(regradeAfter));
        Assert.Equal(HttpStatusCode.Forbidden, studentEdit.StatusCode);
    }

    [Fact]
    public async Task An_assignment_with_submissions_cannot_be_deleted_one_without_can()
    {
        var w = await Campus();
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);
        var due = Factory.Clock.GetUtcNow().AddDays(7);
        var submittedTo = await CreatedAsync(w.HanyClient, w.Cs301, due, "Lab 2");
        var empty = await CreatedAsync(w.HanyClient, w.Cs301, due, "Lab 3");
        await SubmittedAsync(w.AhmedClient, submittedTo);

        Assert.Equal(HttpStatusCode.Conflict, (await w.HanyClient.DeleteAsync($"/api/assignments/{submittedTo}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await w.HanyClient.DeleteAsync($"/api/assignments/{empty}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await w.HanyClient.GetAsync($"/api/assignments/{empty}")).StatusCode);
    }

    [Fact]
    public async Task A_new_assignment_notifies_the_targeted_students()
    {
        var w = await Campus();
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);
        await RegisteredAsync(w.MonaClient, w.Cs301Group2);

        await CreatedAsync(w.HanyClient, w.Cs301, Factory.Clock.GetUtcNow().AddDays(7), subjectGroupId: w.Cs301Group2);

        Assert.Equal(0, await UnreadAsync(w.AhmedClient));
        Assert.Equal(1, await UnreadAsync(w.MonaClient));
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private Task<CampusWorld> Campus() => BuildCampusAsync(Factory, Admin());

    private static async Task<JsonObject> ListAsync(HttpClient client, string query = "") =>
        (await client.GetFromJsonAsync<JsonObject>("/api/assignments" + query))!;

    private static List<string?> Titles(JsonObject page) => page["items"]!.AsArray().Select(a => (string?)a!["title"]).ToList();

    private static async Task<int> UnreadAsync(HttpClient client) =>
        (int)(await client.GetFromJsonAsync<JsonObject>("/api/notifications/mine/unread-count"))!["count"]!;
}
