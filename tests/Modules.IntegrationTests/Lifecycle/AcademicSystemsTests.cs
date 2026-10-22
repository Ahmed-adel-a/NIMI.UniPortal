// ACCEPTANCE TEST — written by the consultant for decision LIFECYCLEDEPT. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Users;
using static NIMI.UniPortal.Modules.IntegrationTests.Grades.GradesSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Lifecycle.RegistrationSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Lifecycle;

/// <summary>
/// Decision LIFECYCLEDEPT: the academic system is chosen per department (department → faculty → university → host
/// default). Both systems run side by side; each serves only its own students; a group may mix both; progress comes
/// from the student's own system; a department with history can't switch.
/// </summary>
public sealed class AcademicSystemsTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task The_system_is_inherited_from_the_university_faculty_or_department()
    {
        var admin = Factory.CreateClientAs("admin");
        var eng = await OrganizationSetup.CreateFacultyAsync(admin, "ENG");
        var cs = await OrganizationSetup.CreateDepartmentAsync(admin, eng, "CS");
        var ee = await OrganizationSetup.CreateDepartmentAsync(admin, eng, "EE");

        Assert.Equal(("credit_hours", "default"), await SystemAsync(admin, cs));

        Assert.Equal(HttpStatusCode.OK, (await SetFacultyAsync(admin, eng, "year_groups")).StatusCode);
        Assert.Equal(("year_groups", "faculty"), await SystemAsync(admin, cs));

        var overview = await BodyOf(await SetDepartmentAsync(admin, ee, "credit_hours"));
        var departments = overview["departments"]!.AsArray();
        Assert.Equal("credit_hours", (string?)departments.Single(d => (Guid)d!["departmentId"]! == ee)!["effective"]);
        Assert.Equal("department", (string?)departments.Single(d => (Guid)d!["departmentId"]! == ee)!["source"]);
        Assert.Equal("year_groups", (string?)departments.Single(d => (Guid)d!["departmentId"]! == cs)!["effective"]);

        Assert.Equal(HttpStatusCode.OK, (await SetFacultyAsync(admin, eng, null)).StatusCode);
        Assert.Equal(("credit_hours", "default"), await SystemAsync(admin, cs));
        Assert.Equal(HttpStatusCode.BadRequest, (await SetDepartmentAsync(admin, cs, "trimesters")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SetDepartmentAsync(Factory.CreateClientAs("student"), cs, "year_groups")).StatusCode);
    }

    [Fact]
    public async Task Each_system_serves_only_its_own_students()
    {
        var admin = Factory.CreateClientAs("admin");
        var w = await BuildAsync(admin);

        Assert.Equal(HttpStatusCode.Forbidden, (await w.SaraClient.GetAsync("/api/registration/offerings")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await RegisterAsync(w.SaraClient, w.Acc101Group)).StatusCode);
        await RegisteredAsync(w.AhmedClient, w.Cs101Group);

        var report = await BodyOf(await admin.PostAsJsonAsync("/api/admin/term-enrollments/generate", new { termId = w.TermId }));
        Assert.Equal(1, (int)report["enrolled"]!);       // Sara's ACC101; Ahmed registers himself
        Assert.Equal(1, (int)report["newStandings"]!);
        Assert.Equal(HttpStatusCode.NotFound, (await w.AhmedClient.GetAsync("/api/standing")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.PutAsJsonAsync($"/api/admin/standings/{w.Ahmed}", new { level = 1 })).StatusCode);

        var me = (await w.SaraClient.GetFromJsonAsync<JsonObject>("/api/auth/me"))!;
        Assert.Equal("year_groups", (string?)me["academicSystem"]);
        Assert.Equal("credit_hours", (string?)(await w.AhmedClient.GetFromJsonAsync<JsonObject>("/api/auth/me"))!["academicSystem"]);
    }

    [Fact]
    public async Task A_group_can_mix_both_systems_and_everyone_sees_it_on_their_schedule()
    {
        var admin = Factory.CreateClientAs("admin");
        var w = await BuildAsync(admin);
        await RegisteredAsync(w.AhmedClient, w.Acc101Group);
        await admin.PostAsJsonAsync("/api/admin/term-enrollments/generate", new { termId = w.TermId });

        var roster = (await w.HanyClient.GetFromJsonAsync<JsonObject>($"/api/teaching/groups/{w.Acc101Group}/roster"))!;
        Assert.Equal(
            new[] { w.Ahmed, w.Sara }.Order(),
            roster["items"]!.AsArray().Select(s => (Guid)s!["studentId"]!).Order());

        foreach (var student in new[] { w.AhmedClient, w.SaraClient })
        {
            var schedule = await student.GetStringAsync("/api/schedule?from=2026-10-03&to=2026-10-16");
            Assert.Contains("ACC101", schedule);
        }
    }

    [Fact]
    public async Task Progress_comes_from_each_students_own_system()
    {
        var admin = Factory.CreateClientAs("admin");
        var w = await BuildAsync(admin);
        await RegisteredAsync(w.AhmedClient, w.Acc101Group);
        await admin.PostAsJsonAsync("/api/admin/term-enrollments/generate", new { termId = w.TermId });

        await GradeAndPublishAsync(w.HanyClient, admin, w.TermId, w.Acc101Group, (w.Ahmed, 85m), (w.Sara, 90m));

        var ahmed = (await w.AhmedClient.GetFromJsonAsync<JsonObject>("/api/registration/progress"))!;
        var sara = (await w.SaraClient.GetFromJsonAsync<JsonObject>("/api/registration/progress"))!;
        Assert.Equal("credit_hours", (string?)ahmed["academicSystem"]);
        Assert.Equal(3, (int)ahmed["creditsEarned"]!);
        Assert.Equal("year_groups", (string?)sara["academicSystem"]);
        Assert.Equal(3, (int)sara["creditsEarned"]!);

        // Credit-hours graduation only lists credit-hours students.
        var eligibility = await admin.GetStringAsync("/api/admin/graduation/eligibility?pageSize=100");
        Assert.Contains(w.Ahmed.ToString(), eligibility);
        Assert.DoesNotContain(w.Sara.ToString(), eligibility);
    }

    [Fact]
    public async Task A_department_whose_students_have_history_cannot_switch_system()
    {
        var admin = Factory.CreateClientAs("admin");
        var w = await BuildAsync(admin);
        await RegisteredAsync(w.AhmedClient, w.Cs101Group);
        var empty = await OrganizationSetup.CreateDepartmentAsync(admin, w.Eng, "ARCH");

        Assert.Equal(HttpStatusCode.Conflict, (await SetDepartmentAsync(admin, w.Cs, "year_groups")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await SetFacultyAsync(admin, w.Eng, "year_groups")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SetDepartmentAsync(admin, empty, "year_groups")).StatusCode);
        Assert.Equal(("credit_hours", "default"), await SystemAsync(admin, w.Cs));
    }

    private async Task<MixedCampus> BuildAsync(HttpClient admin)
    {
        var eng = await OrganizationSetup.CreateFacultyAsync(admin, "ENG");
        var cs = await OrganizationSetup.CreateDepartmentAsync(admin, eng, "CS");
        var com = await OrganizationSetup.CreateFacultyAsync(admin, "COM");
        var acc = await OrganizationSetup.CreateDepartmentAsync(admin, com, "ACC");
        Assert.Equal(HttpStatusCode.OK, (await SetFacultyAsync(admin, com, "year_groups")).StatusCode);

        var (ahmed, ahmedToken) = await AccountSetup.CreateStudentWithTokenAsync(Factory, admin, cs, "ahmed@cu.edu.eg");
        var (sara, saraToken) = await AccountSetup.CreateStudentWithTokenAsync(Factory, admin, acc, "sara@cu.edu.eg");
        var (hany, hanyToken) = await AccountSetup.CreateProfessorWithTokenAsync(Factory, admin, cs, "hany@cu.edu.eg");

        var termId = await CreateActiveTermAsync(admin);
        await OpenWindowAsync(Factory, admin, termId, minCredits: 3);
        var hall = await CreateFacilityAsync(admin, "Hall A", eng);

        var cs101 = await CreateSubjectAsync(admin, cs, "CS101", "Programming", "البرمجة");
        var acc101 = await CreateSubjectAsync(admin, acc, "ACC101", "Accounting", "المحاسبة");
        var cs101Group = await CreateGroupAsync(admin, cs101, termId);
        var acc101Group = await CreateGroupAsync(admin, acc101, termId);
        await AssignTeacherAsync(admin, hany, cs101, termId);
        await AssignTeacherAsync(admin, hany, acc101, termId);
        await CreateSlotAsync(admin, cs101Group, hall, null, 1, "09:00:00", "10:30:00");
        await CreateSlotAsync(admin, acc101Group, hall, null, 2, "09:00:00", "10:30:00");

        var plan = await admin.PutAsJsonAsync($"/api/admin/study-plans/{acc}",
            new { levels = 4, subjects = new[] { new { level = 1, term = "first", subjectId = acc101 } } });
        Assert.Equal(HttpStatusCode.OK, plan.StatusCode);

        return new MixedCampus(
            eng, cs, com, acc, termId, cs101, cs101Group, acc101, acc101Group,
            ahmed, Factory.CreateClientWithToken(ahmedToken),
            sara, Factory.CreateClientWithToken(saraToken),
            hany, Factory.CreateClientWithToken(hanyToken));
    }

    private static async Task<HttpResponseMessage> SetFacultyAsync(HttpClient admin, Guid facultyId, string? academicSystem) =>
        await admin.PutAsJsonAsync($"/api/admin/faculties/{facultyId}/academic-system", new { academicSystem });

    private static async Task<HttpResponseMessage> SetDepartmentAsync(HttpClient admin, Guid departmentId, string? academicSystem) =>
        await admin.PutAsJsonAsync($"/api/admin/departments/{departmentId}/academic-system", new { academicSystem });

    private static async Task<(string System, string Source)> SystemAsync(HttpClient client, Guid departmentId)
    {
        var body = (await client.GetFromJsonAsync<JsonObject>($"/api/settings/academic-system?departmentId={departmentId}"))!;
        return ((string)body["academicSystem"]!, (string)body["source"]!);
    }
}
