// ACCEPTANCE TEST — written by the consultant for phase P09. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Users;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Lifecycle;

/// <summary>P9.4 GRADREQ — Student Affairs sets the credits each department needs to graduate.</summary>
public sealed class GraduationRequirementTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task A_department_starts_unconfigured_and_takes_a_requirement()
    {
        var cs = await DepartmentAsync();

        var initial = (await Admin().GetFromJsonAsync<JsonObject>(Url(cs)))!;
        Assert.Equal(cs, (Guid)initial["departmentId"]!);
        Assert.Null(initial["requiredCredits"]);

        var set = await Admin().PutAsJsonAsync(Url(cs), new { requiredCredits = 132 });
        Assert.Equal(HttpStatusCode.OK, set.StatusCode);
        Assert.Equal(132, (int)(await BodyOf(set))["requiredCredits"]!);

        var changed = await Admin().PutAsJsonAsync(Url(cs), new { requiredCredits = 140 });
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        Assert.Equal(140, (int)(await Admin().GetFromJsonAsync<JsonObject>(Url(cs)))!["requiredCredits"]!);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(401)]
    public async Task Required_credits_are_between_1_and_400(int requiredCredits)
    {
        var cs = await DepartmentAsync();

        var response = await Admin().PutAsJsonAsync(Url(cs), new { requiredCredits });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("requiredCredits", await FieldsOf(response));
    }

    [Fact]
    public async Task An_unknown_department_is_404()
    {
        var ghost = Guid.CreateVersion7();

        Assert.Equal(HttpStatusCode.NotFound, (await Admin().GetAsync(Url(ghost))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Admin().PutAsJsonAsync(Url(ghost), new { requiredCredits = 132 })).StatusCode);
    }

    [Fact]
    public async Task Only_student_affairs_manages_requirements()
    {
        var cs = await DepartmentAsync();
        var (_, token) = await AccountSetup.CreateProfessorWithTokenAsync(Factory, Admin(), cs, "hany@cu.edu.eg");
        var professor = Factory.CreateClientWithToken(token);

        Assert.Equal(HttpStatusCode.Forbidden, (await professor.GetAsync(Url(cs))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await professor.PutAsJsonAsync(Url(cs), new { requiredCredits = 1 })).StatusCode);
    }

    private static string Url(Guid departmentId) => $"/api/admin/graduation/requirements/{departmentId}";

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private async Task<Guid> DepartmentAsync()
    {
        var eng = await OrganizationSetup.CreateFacultyAsync(Admin(), "ENG");
        return await OrganizationSetup.CreateDepartmentAsync(Admin(), eng, "CS");
    }
}
