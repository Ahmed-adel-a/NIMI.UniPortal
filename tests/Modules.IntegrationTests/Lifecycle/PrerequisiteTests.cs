// ACCEPTANCE TEST — written by the consultant for phase P04A. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Users;
using static NIMI.UniPortal.Modules.IntegrationTests.Lifecycle.RegistrationSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Lifecycle;

/// <summary>P4.2 — prerequisites per subject, managed by Student Affairs (decision PREREQ).</summary>
public sealed class PrerequisiteTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task Admin_sets_prerequisites_and_reads_them_back_by_code()
    {
        var s = await Subjects();

        var response = await SetPrerequisitesAsync(Admin(), s.Cs301, s.Math101, s.Cs201);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await BodyOf(response);
        Assert.Equal(s.Cs301, (Guid)saved["subjectId"]!);
        Assert.Equal(["CS201", "MATH101"], Codes(saved));
        var read = (await Admin().GetFromJsonAsync<JsonObject>($"/api/admin/registration/prerequisites/{s.Cs301}"))!;
        Assert.Equal(["CS201", "MATH101"], Codes(read));
        Assert.Equal("Programming", (string?)read["prerequisites"]![0]!["name"]);
        Assert.Equal("البرمجة", (string?)read["prerequisites"]![0]!["nameAr"]);
    }

    [Fact]
    public async Task Saving_replaces_the_whole_list()
    {
        var s = await Subjects();
        await SetPrerequisitesAsync(Admin(), s.Cs301, s.Cs201, s.Math101);

        var response = await SetPrerequisitesAsync(Admin(), s.Cs301, s.Math101);
        var cleared = await SetPrerequisitesAsync(Admin(), s.Cs301);   // empty list

        Assert.Equal(["MATH101"], Codes(await BodyOf(response)));
        Assert.Empty(Codes(await BodyOf(cleared)));
    }

    [Fact]
    public async Task A_subject_cannot_require_itself_an_unknown_subject_or_a_subject_that_requires_it()
    {
        var s = await Subjects();
        await SetPrerequisitesAsync(Admin(), s.Cs301, s.Cs201);

        var itself = await SetPrerequisitesAsync(Admin(), s.Cs301, s.Cs301);
        var unknown = await SetPrerequisitesAsync(Admin(), s.Cs301, Guid.CreateVersion7());
        var cycle = await SetPrerequisitesAsync(Admin(), s.Cs201, s.Cs301);

        foreach (var response in new[] { itself, unknown, cycle })
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("prerequisiteSubjectIds", await FieldsOf(response));
        }
    }

    [Fact]
    public async Task An_unknown_subject_in_the_route_is_a_404()
    {
        var response = await Admin().GetAsync($"/api/admin/registration/prerequisites/{Guid.CreateVersion7()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Only_admins_manage_prerequisites()
    {
        var s = await Subjects();

        var student = await Factory.CreateClientAs("student")
            .PutAsJsonAsync($"/api/admin/registration/prerequisites/{s.Cs301}", new { prerequisiteSubjectIds = new[] { s.Cs201 } });

        Assert.Equal(HttpStatusCode.Forbidden, student.StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private async Task<(Guid Cs201, Guid Cs301, Guid Math101)> Subjects()
    {
        var cs = await OrganizationSetup.CreateDepartmentAsync(Admin(), await OrganizationSetup.CreateFacultyAsync(Admin(), "ENG"), "CS");
        return (
            await CreateSubjectAsync(Admin(), cs, "CS201", "Programming", "البرمجة"),
            await CreateSubjectAsync(Admin(), cs, "CS301", "Algorithms", "الخوارزميات"),
            await CreateSubjectAsync(Admin(), cs, "MATH101", "Calculus", "التفاضل والتكامل"));
    }

    private static List<string?> Codes(JsonObject body) =>
        body["prerequisites"]!.AsArray().Select(p => (string?)p!["code"]).ToList();
}
