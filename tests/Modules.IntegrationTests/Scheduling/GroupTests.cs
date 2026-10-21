// ACCEPTANCE TEST — written by the consultant for phase P03. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Users;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Scheduling;

/// <summary>P3.3 — the groups (شعب) a subject offers in a term (rule R4).</summary>
public sealed class GroupTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task Admin_opens_groups_and_they_are_listed_by_number()
    {
        var (subjectId, termId, _) = await Catalog();

        var second = await PostGroupAsync(Admin(), subjectId, termId, groupNumber: 2, capacity: 40);
        var first = await PostGroupAsync(Admin(), subjectId, termId, groupNumber: 1, capacity: 60);

        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var group = await BodyOf(second);
        Assert.Equal("CS301", (string?)group["subjectCode"]);
        Assert.Equal(termId, (Guid)group["termId"]!);
        Assert.Equal(40, (int)group["capacity"]!);
        Assert.Equal(0, (int)group["registeredCount"]!);
        Assert.True((bool)group["isActive"]!);
        var listed = (await Student().GetFromJsonAsync<JsonObject>($"/api/subjects/{subjectId}/groups?termId={termId}"))!;
        Assert.Equal([1, 2], listed["items"]!.AsArray().Select(g => (int)g!["groupNumber"]!));
    }

    [Fact]
    public async Task The_same_group_number_twice_in_a_term_is_a_409()
    {
        var (subjectId, termId, _) = await Catalog();
        await CreateGroupAsync(Admin(), subjectId, termId, 1);

        var duplicate = await PostGroupAsync(Admin(), subjectId, termId, 1);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Groups_cannot_be_opened_in_a_closed_term()
    {
        var (subjectId, termId, _) = await Catalog();
        await Admin().PostAsync($"/api/admin/terms/{termId}/close", null);

        var response = await PostGroupAsync(Admin(), subjectId, termId);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("termId", await FieldsOf(response));
    }

    [Fact]
    public async Task Groups_cannot_be_opened_for_an_inactive_subject()
    {
        var (subjectId, termId, _) = await Catalog();
        await Admin().PutAsJsonAsync($"/api/admin/subjects/{subjectId}", new
        {
            name = "Algorithms", nameAr = "الخوارزميات", description = (string?)null, creditHours = 3,
            category = "regular", isGraduationRequirement = false, isActive = false,
        });

        var response = await PostGroupAsync(Admin(), subjectId, termId);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("subjectId", await FieldsOf(response));
    }

    [Fact]
    public async Task Invalid_capacity_is_a_400()
    {
        var (subjectId, termId, _) = await Catalog();

        var response = await PostGroupAsync(Admin(), subjectId, termId, capacity: 0);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("capacity", await FieldsOf(response));
    }

    [Fact]
    public async Task Admin_updates_capacity_target_department_and_status()
    {
        var (subjectId, termId, cs) = await Catalog();
        var groupId = await CreateGroupAsync(Admin(), subjectId, termId);

        var response = await Admin().PutAsJsonAsync($"/api/admin/groups/{groupId}", new { capacity = 80, targetDepartmentId = cs, isActive = false });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var group = await BodyOf(response);
        Assert.Equal(80, (int)group["capacity"]!);
        Assert.Equal(cs, (Guid)group["targetDepartmentId"]!);
        Assert.False((bool)group["isActive"]!);
    }

    [Fact]
    public async Task Unknown_subject_or_group_is_a_404()
    {
        var (_, termId, _) = await Catalog();

        Assert.Equal(HttpStatusCode.NotFound, (await PostGroupAsync(Admin(), Guid.CreateVersion7(), termId)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Admin().PutAsJsonAsync($"/api/admin/groups/{Guid.CreateVersion7()}",
            new { capacity = 80, targetDepartmentId = (Guid?)null, isActive = true })).StatusCode);
    }

    [Fact]
    public async Task Only_admins_open_groups()
    {
        var (subjectId, termId, _) = await Catalog();

        var response = await Factory.CreateClientAs("professor")
            .PostAsJsonAsync($"/api/admin/subjects/{subjectId}/groups", new { termId, groupNumber = 1, capacity = 60 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private HttpClient Student() => Factory.CreateClientAs("student");

    private async Task<(Guid SubjectId, Guid TermId, Guid DepartmentId)> Catalog()
    {
        var cs = await OrganizationSetup.CreateDepartmentAsync(Admin(), await OrganizationSetup.CreateFacultyAsync(Admin(), "ENG"), "CS");
        return (await CreateSubjectAsync(Admin(), cs, "CS301"), await CreateActiveTermAsync(Admin()), cs);
    }
}
