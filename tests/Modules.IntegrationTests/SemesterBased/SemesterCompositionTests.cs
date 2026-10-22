// ACCEPTANCE TEST — written by the consultant for phase P4.B, revised for decision LIFECYCLEDEPT. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Users;

namespace NIMI.UniPortal.Modules.IntegrationTests.SemesterBased;

/// <summary>
/// Decision LIFECYCLEDEPT: the reference host installs both academic systems like every deployment, and its own
/// default (appsettings: <c>Platform:DefaultAcademicSystem</c>) puts departments on year groups.
/// </summary>
public sealed class SemesterCompositionTests(ReferenceHostFactory factory) : ReferenceHostTest(factory)
{
    [Fact]
    public async Task The_reference_host_defaults_to_year_groups_so_self_registration_is_refused()
    {
        var admin = Factory.CreateClientAs("admin");
        var eng = await OrganizationSetup.CreateFacultyAsync(admin, "ENG");
        var cs = await OrganizationSetup.CreateDepartmentAsync(admin, eng, "CS");
        var (_, token) = await AccountSetup.CreateStudentWithTokenAsync(Factory, admin, cs, "ahmed@asu.edu.eg");

        var system = (await admin.GetFromJsonAsync<JsonObject>($"/api/settings/academic-system?departmentId={cs}"))!;
        Assert.Equal("year_groups", (string?)system["academicSystem"]);
        Assert.Equal("default", (string?)system["source"]);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await Factory.CreateClientWithToken(token).GetAsync("/api/registration/offerings")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/admin/promotion-policy")).StatusCode);
    }

    [Fact]
    public async Task The_shared_student_routes_answer_like_credit_hours()
    {
        var student = Factory.CreateClientAs("student");

        Assert.Equal(HttpStatusCode.OK, (await student.GetAsync("/api/registration/progress")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await student.GetAsync("/api/schedule?from=2027-02-06&to=2027-02-12")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await student.GetAsync("/api/standing")).StatusCode);
    }
}
