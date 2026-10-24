// ACCEPTANCE TEST — written by the consultant for decision ROLES2 (student reports). Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Users;

namespace NIMI.UniPortal.Modules.IntegrationTests.StudentReports;

/// <summary>
/// Decision ROLES2: a student sends a report to the head of their department; the head reads their department's
/// reports and answers once and/or closes them; the student follows their own; Student Affairs reads them all.
/// </summary>
public sealed class StudentReportTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task A_student_reports_to_their_department_head_who_is_notified()
    {
        var c = await CampusAsync();

        var sent = await Submit(c.Ahmed, "Lab computers are broken", category: "academic");
        Assert.Equal(HttpStatusCode.Created, sent.StatusCode);
        var report = (await sent.Content.ReadFromJsonAsync<JsonObject>())!;
        Assert.Equal("open", (string?)report["status"]);
        Assert.Equal("academic", (string?)report["category"]);
        Assert.Equal(c.Cs, (Guid)report["departmentId"]!);

        var mine = (await c.Ahmed.GetFromJsonAsync<JsonObject>("/api/reports/mine"))!;
        Assert.Equal(1, (int)mine["totalCount"]!);
        var unread = (await c.CsHead.GetFromJsonAsync<JsonObject>("/api/notifications/mine/unread-count"))!;
        Assert.Equal(1, (int)unread["count"]!);
    }

    [Fact]
    public async Task A_head_sees_only_their_own_departments_reports()
    {
        var c = await CampusAsync();
        await Submit(c.Ahmed, "From CS");
        await Submit(c.Sara, "From EE");

        var cs = (await c.CsHead.GetFromJsonAsync<JsonObject>("/api/department/reports"))!;
        Assert.Equal("From CS", (string?)cs["items"]!.AsArray().Single()!["title"]);
        Assert.Equal("Ahmed Student", (string?)cs["items"]![0]!["studentName"]);

        Assert.Equal(HttpStatusCode.Forbidden, (await c.Ahmed.GetAsync("/api/department/reports")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Factory.CreateClientAs("professor").GetAsync("/api/department/reports")).StatusCode);
    }

    [Fact]
    public async Task The_head_answers_once_then_closes_and_the_student_is_told()
    {
        var c = await CampusAsync();
        var id = await SubmittedAsync(c.Ahmed, "Exam clash");

        var answered = await Answer(c.CsHead, id, reply: "We moved your exam to Sunday.");
        Assert.Equal(HttpStatusCode.OK, answered.StatusCode);
        var body = (await answered.Content.ReadFromJsonAsync<JsonObject>())!;
        Assert.Equal("answered", (string?)body["status"]);
        Assert.Equal("We moved your exam to Sunday.", (string?)body["reply"]);
        Assert.Equal(1, (int)(await c.Ahmed.GetFromJsonAsync<JsonObject>("/api/notifications/mine/unread-count"))!["count"]!);

        Assert.Equal(HttpStatusCode.Conflict, (await Answer(c.CsHead, id, reply: "Second reply")).StatusCode);

        var closed = await Answer(c.CsHead, id, close: true);
        Assert.Equal(HttpStatusCode.OK, closed.StatusCode);
        Assert.Equal("closed", (string?)(await closed.Content.ReadFromJsonAsync<JsonObject>())!["status"]);
        Assert.Equal(HttpStatusCode.Conflict, (await Answer(c.CsHead, id, close: true)).StatusCode);

        var seen = (await c.Ahmed.GetFromJsonAsync<JsonObject>($"/api/reports/{id}"))!;
        Assert.Equal("closed", (string?)seen["status"]);
        Assert.Equal("We moved your exam to Sunday.", (string?)seen["reply"]);
    }

    [Fact]
    public async Task A_report_can_be_answered_and_closed_in_one_step()
    {
        var c = await CampusAsync();
        var id = await SubmittedAsync(c.Ahmed, "Lost ID card");

        var done = await Answer(c.CsHead, id, reply: "Collect it from the office.", close: true);

        Assert.Equal(HttpStatusCode.OK, done.StatusCode);
        Assert.Equal("closed", (string?)(await done.Content.ReadFromJsonAsync<JsonObject>())!["status"]);
        Assert.Equal(HttpStatusCode.BadRequest, (await Answer(c.CsHead, await SubmittedAsync(c.Ahmed, "Empty answer"))).StatusCode);
    }

    [Fact]
    public async Task Only_the_student_their_head_and_student_affairs_can_see_a_report()
    {
        var c = await CampusAsync();
        var id = await SubmittedAsync(c.Ahmed, "Private matter");

        Assert.Equal(HttpStatusCode.OK, (await c.CsHead.GetAsync($"/api/reports/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.Admin.GetAsync($"/api/reports/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.Sara.GetAsync($"/api/reports/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.EeHead.GetAsync($"/api/reports/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Answer(c.EeHead, id, reply: "Not mine")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Answer(c.Admin, id, reply: "Student Affairs only reads")).StatusCode);
    }

    [Fact]
    public async Task Student_affairs_reads_every_report_and_filters_them()
    {
        var c = await CampusAsync();
        await Submit(c.Ahmed, "One");
        var closedId = await SubmittedAsync(c.Ahmed, "Two");
        await Answer(c.CsHead, closedId, close: true);
        await Submit(c.Sara, "Three");

        Assert.Equal(3, (int)(await c.Admin.GetFromJsonAsync<JsonObject>("/api/admin/reports"))!["totalCount"]!);
        Assert.Equal(2, (int)(await c.Admin.GetFromJsonAsync<JsonObject>($"/api/admin/reports?departmentId={c.Cs}"))!["totalCount"]!);
        Assert.Equal(1, (int)(await c.Admin.GetFromJsonAsync<JsonObject>("/api/admin/reports?status=closed"))!["totalCount"]!);
        Assert.Equal(1, (int)(await c.CsHead.GetFromJsonAsync<JsonObject>("/api/department/reports?status=open"))!["totalCount"]!);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.Ahmed.GetAsync("/api/admin/reports")).StatusCode);
    }

    [Fact]
    public async Task A_department_without_a_head_still_takes_reports_and_its_next_head_sees_them()
    {
        var admin = Factory.CreateClientAs("admin");
        var faculty = await OrganizationSetup.CreateFacultyAsync(admin, "ART");
        var history = await OrganizationSetup.CreateDepartmentAsync(admin, faculty, "HIS");
        var (_, token) = await AccountSetup.CreateStudentWithTokenAsync(Factory, admin, history, "student@cu.edu.eg");
        var student = Factory.CreateClientWithToken(token);

        Assert.Equal(HttpStatusCode.Created, (await Submit(student, "Nobody to ask")).StatusCode);
        Assert.Equal(1, (int)(await admin.GetFromJsonAsync<JsonObject>("/api/admin/reports"))!["totalCount"]!);

        var (_, headToken) = await AccountSetup.CreateDepartmentHeadWithTokenAsync(Factory, admin, history, "his.head@cu.edu.eg");
        var inbox = (await Factory.CreateClientWithToken(headToken).GetFromJsonAsync<JsonObject>("/api/department/reports"))!;
        Assert.Equal(1, (int)inbox["totalCount"]!);
    }

    [Fact]
    public async Task Reports_are_validated_and_only_students_send_them()
    {
        var c = await CampusAsync();

        Assert.Equal(HttpStatusCode.BadRequest, (await Submit(c.Ahmed, "")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Submit(c.Ahmed, "Title", message: "")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Submit(c.Ahmed, "Title", category: "gossip")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Submit(c.CsHead, "Heads don't report to themselves")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Submit(c.Admin, "Student Affairs neither")).StatusCode);
    }

    private static Task<HttpResponseMessage> Submit(HttpClient client, string title, string message = "Please help.", string category = "other") =>
        client.PostAsJsonAsync("/api/reports", new { category, title, message });

    private static async Task<Guid> SubmittedAsync(HttpClient client, string title)
    {
        var sent = await Submit(client, title);
        Assert.Equal(HttpStatusCode.Created, sent.StatusCode);
        return (Guid)(await sent.Content.ReadFromJsonAsync<JsonObject>())!["id"]!;
    }

    private static Task<HttpResponseMessage> Answer(HttpClient client, Guid id, string? reply = null, bool close = false) =>
        client.PostAsJsonAsync($"/api/department/reports/{id}/answer", new { reply, close });

    private async Task<ReportCampus> CampusAsync()
    {
        var admin = Factory.CreateClientAs("admin");
        var eng = await OrganizationSetup.CreateFacultyAsync(admin, "ENG");
        var cs = await OrganizationSetup.CreateDepartmentAsync(admin, eng, "CS");
        var ee = await OrganizationSetup.CreateDepartmentAsync(admin, eng, "EE");
        var ahmed = await StudentAsync(admin, cs, "ahmed@cu.edu.eg", "Ahmed Student");
        var sara = await StudentAsync(admin, ee, "sara@cu.edu.eg", "Sara Student");
        var (_, csHead) = await AccountSetup.CreateDepartmentHeadWithTokenAsync(Factory, admin, cs, "cs.head@cu.edu.eg");
        var (_, eeHead) = await AccountSetup.CreateDepartmentHeadWithTokenAsync(Factory, admin, ee, "ee.head@cu.edu.eg");
        return new ReportCampus(
            admin, cs, ahmed, sara, Factory.CreateClientWithToken(csHead), Factory.CreateClientWithToken(eeHead));
    }

    private async Task<HttpClient> StudentAsync(HttpClient admin, Guid departmentId, string email, string fullName)
    {
        var created = await admin.PostAsJsonAsync("/api/admin/users", new
        {
            role = "student", fullName, email, password = "Portal!Pass2026", departmentId, enrollmentYear = 2026,
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var login = (await (await Factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email, password = "Portal!Pass2026" }))
            .Content.ReadFromJsonAsync<JsonObject>())!;
        var change = await Factory.CreateClientWithToken((string)login["accessToken"]!)
            .PostAsJsonAsync("/api/auth/change-password", new { currentPassword = "Portal!Pass2026", newPassword = "Portal!Pass2027" });
        return Factory.CreateClientWithToken((string)(await change.Content.ReadFromJsonAsync<JsonObject>())!["accessToken"]!);
    }
}
