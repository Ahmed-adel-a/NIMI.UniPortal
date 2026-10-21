// ACCEPTANCE TEST — written by the consultant for phase P04A. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Lifecycle.RegistrationSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Lifecycle;

/// <summary>P4.4 — group rosters (rule R11: only confirmed members; ENROLL: the lifecycle owns rosters).</summary>
public sealed class RosterTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task The_teaching_professor_sees_the_registered_students_ordered_by_code()
    {
        var w = await Campus();
        await RegisteredAsync(w.MonaClient, w.Cs301Group1);
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);

        var response = await w.HanyClient.GetAsync($"/api/teaching/groups/{w.Cs301Group1}/roster");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var roster = await BodyOf(response);
        Assert.Equal(2, (int)roster["totalCount"]!);
        var students = roster["items"]!.AsArray().Select(s => s!).ToList();
        Assert.Equal(new[] { w.Ahmed, w.Mona }.Order(), students.Select(s => (Guid)s["studentId"]!).Order());
        Assert.All(students, s => Assert.Equal("Test Student", (string?)s["fullName"]));
        var codes = students.Select(s => (string)s["studentCode"]!).ToList();
        Assert.Equal(codes.Order(StringComparer.Ordinal), codes);
    }

    [Fact]
    public async Task Dropped_students_leave_the_roster()
    {
        var w = await Campus();
        var registration = await RegisteredAsync(w.AhmedClient, w.Cs301Group1);
        await RegisteredAsync(w.MonaClient, w.Cs301Group1);

        await w.AhmedClient.DeleteAsync($"/api/registration/{registration}");
        var roster = (await w.HanyClient.GetFromJsonAsync<JsonObject>($"/api/teaching/groups/{w.Cs301Group1}/roster"))!;

        Assert.Equal([w.Mona], roster["items"]!.AsArray().Select(s => (Guid)s!["studentId"]!));
    }

    [Fact]
    public async Task A_professor_assigned_to_one_group_sees_only_that_groups_roster()
    {
        var w = await Campus();
        var (lab, labToken) = await AccountSetup.CreateProfessorWithTokenAsync(Factory, Admin(), w.CsDepartmentId, "lab.ta@cu.edu.eg");
        await AssignTeacherAsync(Admin(), lab, w.Cs301, w.TermId, w.Cs301Group2, "lab");
        var labClient = Factory.CreateClientWithToken(labToken);

        Assert.Equal(HttpStatusCode.OK, (await labClient.GetAsync($"/api/teaching/groups/{w.Cs301Group2}/roster")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await labClient.GetAsync($"/api/teaching/groups/{w.Cs301Group1}/roster")).StatusCode);
    }

    [Fact]
    public async Task Professors_who_do_not_teach_the_group_and_students_are_refused()
    {
        var w = await Campus();
        var (_, otherToken) = await AccountSetup.CreateProfessorWithTokenAsync(Factory, Admin(), w.CsDepartmentId, "other@cu.edu.eg");

        var otherProfessor = await Factory.CreateClientWithToken(otherToken).GetAsync($"/api/teaching/groups/{w.Cs301Group1}/roster");
        var student = await w.AhmedClient.GetAsync($"/api/teaching/groups/{w.Cs301Group1}/roster");

        Assert.Equal(HttpStatusCode.Forbidden, otherProfessor.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, student.StatusCode);
    }

    [Fact]
    public async Task Admin_sees_any_roster_and_an_unknown_group_is_a_404()
    {
        var w = await Campus();
        await RegisteredAsync(w.AhmedClient, w.Cs303Group1);

        var roster = (await Admin().GetFromJsonAsync<JsonObject>($"/api/admin/groups/{w.Cs303Group1}/roster"))!;
        var unknown = await Admin().GetAsync($"/api/admin/groups/{Guid.CreateVersion7()}/roster");

        Assert.Equal([w.Ahmed], roster["items"]!.AsArray().Select(s => (Guid)s!["studentId"]!));
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await w.HanyClient.GetAsync($"/api/admin/groups/{w.Cs303Group1}/roster")).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private Task<CampusWorld> Campus() => BuildCampusAsync(Factory, Admin());
}
