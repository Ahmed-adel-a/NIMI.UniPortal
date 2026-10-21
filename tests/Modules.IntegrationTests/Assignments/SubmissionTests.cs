// ACCEPTANCE TEST — written by the consultant for phase P08. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Lifecycle;
using static NIMI.UniPortal.Modules.IntegrationTests.Assignments.AssignmentsSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Lifecycle.RegistrationSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Assignments;

/// <summary>P8.3–P8.6 — submissions with receipts, the teacher's view, one-time review with notification, and "mine" (R3–R8).</summary>
public sealed class SubmissionTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task A_registered_student_submits_before_the_deadline_and_gets_a_receipt()
    {
        var w = await Campus();
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);
        var assignment = await CreatedAsync(w.HanyClient, w.Cs301, Factory.Clock.GetUtcNow().AddDays(7));

        var response = await SubmitAsync(w.AhmedClient, assignment);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var receipt = await BodyOf(response);
        Assert.Matches(new Regex(@"^SUB-\d{4}-\d{6}$"), (string)receipt["receiptNumber"]!);
        Assert.Equal("submitted", (string?)receipt["status"]);
        Assert.NotNull((string?)receipt["submittedAt"]);
    }

    [Fact]
    public async Task Receipt_numbers_are_sequential()
    {
        var w = await Campus();
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);
        await RegisteredAsync(w.MonaClient, w.Cs301Group2);
        var assignment = await CreatedAsync(w.HanyClient, w.Cs301, Factory.Clock.GetUtcNow().AddDays(7));

        var first = (string)(await BodyOf(await SubmitAsync(w.AhmedClient, assignment)))["receiptNumber"]!;
        var second = (string)(await BodyOf(await SubmitAsync(w.MonaClient, assignment)))["receiptNumber"]!;

        Assert.Equal(int.Parse(first[^6..]) + 1, int.Parse(second[^6..]));
    }

    [Fact]
    public async Task The_deadline_is_enforced_by_the_server_clock()
    {
        var w = await Campus();
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);
        var assignment = await CreatedAsync(w.HanyClient, w.Cs301, Factory.Clock.GetUtcNow().AddHours(1));

        Factory.Clock.Advance(TimeSpan.FromHours(2));
        var response = await SubmitAsync(w.AhmedClient, assignment);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("assignmentId", await FieldsOf(response));
        Assert.Contains("The deadline has passed.", (await BodyOf(response))["details"]!.AsArray().Select(d => (string?)d!["error"]));
    }

    [Fact]
    public async Task One_submission_per_student_and_the_file_rules_apply()
    {
        var w = await Campus();
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);
        await RegisteredAsync(w.MonaClient, w.Cs301Group2);
        var assignment = await CreatedAsync(w.HanyClient, w.Cs301, Factory.Clock.GetUtcNow().AddDays(7));
        await SubmittedAsync(w.AhmedClient, assignment);

        var twice = await SubmitAsync(w.AhmedClient, assignment);
        var wrongType = await SubmitAsync(w.MonaClient, assignment, "lab2.exe", contentType: "application/octet-stream");
        var empty = await SubmitAsync(w.MonaClient, assignment, "lab2.pdf", content: []);

        Assert.Equal(HttpStatusCode.Conflict, twice.StatusCode);
        Assert.Contains("file", await FieldsOf(wrongType));
        Assert.Contains("file", await FieldsOf(empty));
    }

    [Fact]
    public async Task Only_students_who_can_see_the_assignment_submit()
    {
        var w = await Campus();
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);
        var groupTwoOnly = await CreatedAsync(w.HanyClient, w.Cs301, Factory.Clock.GetUtcNow().AddDays(7), subjectGroupId: w.Cs301Group2);

        Assert.Equal(HttpStatusCode.NotFound, (await SubmitAsync(w.AhmedClient, groupTwoOnly)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await SubmitAsync(w.MonaClient, groupTwoOnly)).StatusCode);       // not registered at all
        Assert.Equal(HttpStatusCode.Forbidden, (await SubmitAsync(w.HanyClient, groupTwoOnly)).StatusCode);
    }

    [Fact]
    public async Task The_teacher_lists_submissions_and_downloads_them_and_the_owner_can_too()
    {
        var w = await Campus();
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);
        await RegisteredAsync(w.MonaClient, w.Cs301Group2);
        var assignment = await CreatedAsync(w.HanyClient, w.Cs301, Factory.Clock.GetUtcNow().AddDays(7));
        var ahmeds = await SubmittedAsync(w.AhmedClient, assignment);
        Factory.Clock.Advance(TimeSpan.FromMinutes(1));
        await SubmittedAsync(w.MonaClient, assignment, "mona-lab2.pdf");

        var list = (await w.HanyClient.GetFromJsonAsync<JsonObject>($"/api/assignments/{assignment}/submissions"))!;
        var teacherFile = await w.HanyClient.GetAsync($"/api/submissions/{ahmeds}/file");
        var ownerFile = await w.AhmedClient.GetAsync($"/api/submissions/{ahmeds}/file");
        var otherStudent = await w.MonaClient.GetAsync($"/api/submissions/{ahmeds}/file");

        Assert.Equal(2, (int)list["totalCount"]!);
        var first = list["items"]![0]!;
        Assert.Equal(w.Ahmed, (Guid)first["studentId"]!);
        Assert.Equal("Test Student", (string?)first["studentName"]);
        Assert.NotNull((string?)first["studentCode"]);
        Assert.Equal("lab2.pdf", (string?)first["fileName"]);
        Assert.Equal(HttpStatusCode.OK, teacherFile.StatusCode);
        Assert.Equal(ReportBytes, await teacherFile.Content.ReadAsByteArrayAsync());
        Assert.Equal(HttpStatusCode.OK, ownerFile.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, otherStudent.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await w.AhmedClient.GetAsync($"/api/assignments/{assignment}/submissions")).StatusCode);
    }

    [Fact]
    public async Task Approving_records_the_grade_and_notifies_the_student()
    {
        var w = await Campus();
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);
        var assignment = await CreatedAsync(w.HanyClient, w.Cs301, Factory.Clock.GetUtcNow().AddDays(7), maxGrade: 10m);
        var submission = await SubmittedAsync(w.AhmedClient, assignment);
        var unreadBefore = await UnreadAsync(w.AhmedClient);

        var response = await ReviewAsync(w.HanyClient, submission, "approve", grade: 8.5m, feedback: "عمل ممتاز");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var reviewed = await BodyOf(response);
        Assert.Equal("approved", (string?)reviewed["status"]);
        Assert.Equal(8.5m, (decimal)reviewed["grade"]!);
        Assert.Equal("عمل ممتاز", (string?)reviewed["feedback"]);
        Assert.NotNull((string?)reviewed["reviewedAt"]);
        Assert.Equal(unreadBefore + 1, await UnreadAsync(w.AhmedClient));
    }

    [Fact]
    public async Task Review_rules_grade_bounds_a_reason_to_reject_and_only_once()
    {
        var w = await Campus();
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);
        await RegisteredAsync(w.MonaClient, w.Cs301Group2);
        var assignment = await CreatedAsync(w.HanyClient, w.Cs301, Factory.Clock.GetUtcNow().AddDays(7), maxGrade: 10m);
        var ahmeds = await SubmittedAsync(w.AhmedClient, assignment);
        var monas = await SubmittedAsync(w.MonaClient, assignment);

        var tooHigh = await ReviewAsync(w.HanyClient, ahmeds, "approve", grade: 10.5m);
        var noReason = await ReviewAsync(w.HanyClient, monas, "reject");
        var rejected = await ReviewAsync(w.HanyClient, monas, "reject", rejectionReason: "Wrong file uploaded");
        var again = await ReviewAsync(w.HanyClient, monas, "approve", grade: 5m);
        var byStudent = await ReviewAsync(w.AhmedClient, ahmeds, "approve", grade: 10m);

        Assert.Contains("grade", await FieldsOf(tooHigh));
        Assert.Contains("rejectionReason", await FieldsOf(noReason));
        Assert.Equal("rejected", (string?)(await BodyOf(rejected))["status"]);
        Assert.Equal("Wrong file uploaded", (string?)(await BodyOf(rejected))["feedback"]);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, byStudent.StatusCode);
    }

    [Fact]
    public async Task Students_track_their_own_submissions_newest_first()
    {
        var w = await Campus();
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);
        await RegisteredAsync(w.MonaClient, w.Cs301Group2);
        var lab2 = await CreatedAsync(w.HanyClient, w.Cs301, Factory.Clock.GetUtcNow().AddDays(7), "Lab 2", maxGrade: 10m);
        var lab3 = await CreatedAsync(w.HanyClient, w.Cs301, Factory.Clock.GetUtcNow().AddDays(8), "Lab 3", maxGrade: 20m);
        var first = await SubmittedAsync(w.AhmedClient, lab2);
        Factory.Clock.Advance(TimeSpan.FromMinutes(5));
        await SubmittedAsync(w.AhmedClient, lab3);
        await SubmittedAsync(w.MonaClient, lab2);
        await ReviewAsync(w.HanyClient, first, "approve", grade: 9m);

        var mine = (await w.AhmedClient.GetFromJsonAsync<JsonObject>("/api/submissions/mine"))!;

        Assert.Equal(2, (int)mine["totalCount"]!);
        var items = mine["items"]!.AsArray();
        Assert.Equal(["Lab 3", "Lab 2"], items.Select(s => (string?)s!["assignmentTitle"]));
        Assert.Equal("CS301", (string?)items[0]!["subjectCode"]);
        Assert.Equal("approved", (string?)items[1]!["status"]);
        Assert.Equal(9m, (decimal)items[1]!["grade"]!);
        Assert.Equal(10m, (decimal)items[1]!["maxGrade"]!);
        Assert.Matches(new Regex(@"^SUB-\d{4}-\d{6}$"), (string)items[1]!["receiptNumber"]!);
        Assert.Equal(HttpStatusCode.Forbidden, (await w.HanyClient.GetAsync("/api/submissions/mine")).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private Task<CampusWorld> Campus() => BuildCampusAsync(Factory, Admin());

    private static async Task<int> UnreadAsync(HttpClient client) =>
        (int)(await client.GetFromJsonAsync<JsonObject>("/api/notifications/mine/unread-count"))!["count"]!;
}
