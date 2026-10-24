// ACCEPTANCE TEST — written by the consultant for decision ROLES2 (department head). Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Users;

/// <summary>
/// Decision ROLES2: Student Affairs creates a department head for one department (its own kind of account, one active
/// head per department). A head may be assigned to teach a subject and then works like any teaching staff for it.
/// </summary>
public sealed class DepartmentHeadTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    private const string Url = "/api/admin/users";

    [Fact]
    public async Task Student_affairs_creates_a_department_head_who_signs_in_as_one()
    {
        var admin = Factory.CreateClientAs("admin");
        var cs = await Department(admin, "CS");

        var created = await admin.PostAsJsonAsync(Url, new { role = "department_head", fullName = "Dr. Hala Nabil", email = "hala@cu.edu.eg", departmentId = cs });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var user = (await created.Content.ReadFromJsonAsync<JsonObject>())!["user"]!;
        Assert.Equal("department_head", (string?)user["role"]);
        Assert.Equal(cs, (Guid)user["departmentId"]!);

        var listed = (await admin.GetFromJsonAsync<JsonObject>($"{Url}?role=department_head"))!;
        Assert.Equal("hala@cu.edu.eg", (string?)listed["items"]!.AsArray().Single()!["email"]);

        var (_, token) = await AccountSetup.CreateDepartmentHeadWithTokenAsync(Factory, admin, await Department(admin, "EE"), "ee.head@cu.edu.eg");
        var me = (await Factory.CreateClientWithToken(token).GetFromJsonAsync<JsonObject>("/api/auth/me"))!;
        Assert.Equal("department_head", (string?)me["role"]);
        Assert.NotNull(me["departmentId"]);
    }

    [Fact]
    public async Task A_department_head_needs_a_department_and_has_no_student_fields()
    {
        var admin = Factory.CreateClientAs("admin");
        var cs = await Department(admin, "CS");

        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.PostAsJsonAsync(Url, new { role = "department_head", fullName = "No Department", email = "a@cu.edu.eg" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.PostAsJsonAsync(Url, new { role = "department_head", fullName = "With Year", email = "b@cu.edu.eg", departmentId = cs, enrollmentYear = 2026 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await Factory.CreateClientAs("professor").PostAsJsonAsync(Url, new { role = "department_head", fullName = "X", email = "c@cu.edu.eg", departmentId = cs })).StatusCode);
    }

    [Fact]
    public async Task A_department_has_at_most_one_active_head()
    {
        var admin = Factory.CreateClientAs("admin");
        var cs = await Department(admin, "CS");
        var first = await Head(admin, cs, "first@cu.edu.eg");

        var second = await admin.PostAsJsonAsync(Url, new { role = "department_head", fullName = "Second Head", email = "second@cu.edu.eg", departmentId = cs });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await admin.PatchAsJsonAsync($"{Url}/{first}/status", new { isActive = false })).StatusCode);
        Assert.Equal(HttpStatusCode.Created,
            (await admin.PostAsJsonAsync(Url, new { role = "department_head", fullName = "Second Head", email = "second@cu.edu.eg", departmentId = cs })).StatusCode);

        // Reactivating the old head would give the department two.
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PatchAsJsonAsync($"{Url}/{first}/status", new { isActive = true })).StatusCode);
    }

    [Fact]
    public async Task A_head_assigned_to_a_subject_teaches_it_like_any_teaching_staff()
    {
        var admin = Factory.CreateClientAs("admin");
        var eng = await OrganizationSetup.CreateFacultyAsync(admin, "ENG");
        var cs = await OrganizationSetup.CreateDepartmentAsync(admin, eng, "CS");
        var ee = await OrganizationSetup.CreateDepartmentAsync(admin, eng, "EE");
        var (head, headToken) = await AccountSetup.CreateDepartmentHeadWithTokenAsync(Factory, admin, cs, "cs.head@cu.edu.eg");
        var (_, otherHeadToken) = await AccountSetup.CreateDepartmentHeadWithTokenAsync(Factory, admin, ee, "ee.head@cu.edu.eg");
        var termId = await CreateActiveTermAsync(admin);
        var subject = await CreateSubjectAsync(admin, cs, "CS101", "Programming", "البرمجة");
        var group = await CreateGroupAsync(admin, subject, termId);

        Assert.Equal(HttpStatusCode.Created, (await PostAssignmentAsync(admin, head, subject, termId, group)).StatusCode);

        var headClient = Factory.CreateClientWithToken(headToken);
        Assert.Equal(HttpStatusCode.Created, (await Announce(headClient, subject)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await headClient.GetAsync("/api/teaching/schedule?from=2026-10-03&to=2026-10-16")).StatusCode);

        // A head who doesn't teach the subject can't post to it.
        Assert.Equal(HttpStatusCode.Forbidden, (await Announce(Factory.CreateClientWithToken(otherHeadToken), subject)).StatusCode);
    }

    private static Task<HttpResponseMessage> Announce(HttpClient client, Guid subjectId) =>
        client.PostAsJsonAsync("/api/announcements", new { subjectId, title = "Midterm moved", body = "Now on Monday.", priority = "normal", isPinned = false });

    private static async Task<Guid> Head(HttpClient admin, Guid departmentId, string email)
    {
        var created = await admin.PostAsJsonAsync(Url, new { role = "department_head", fullName = "Head", email, departmentId });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        return (Guid)(await created.Content.ReadFromJsonAsync<JsonObject>())!["user"]!["id"]!;
    }

    private static async Task<Guid> Department(HttpClient admin, string code)
    {
        var faculty = await OrganizationSetup.CreateFacultyAsync(admin, "F" + code);
        return await OrganizationSetup.CreateDepartmentAsync(admin, faculty, code);
    }
}
