using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;

namespace NIMI.UniPortal.Modules.IntegrationTests.Users;

/// <summary>Departments (US-A11 part 1): always inside a faculty; codes unique across the university.</summary>
public sealed class DepartmentTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task Admin_creates_departments_and_lists_them_per_faculty()
    {
        var engineering = await CreateFaculty("ENG");
        var commerce = await CreateFaculty("COM");
        await CreateDepartment(engineering, "cs", "Computer Science", "علوم الحاسب");
        await CreateDepartment(engineering, "CIV", "Civil", "مدني");
        await CreateDepartment(commerce, "ACC", "Accounting", "محاسبة");

        var list = (await Factory.CreateClientAs("student").GetFromJsonAsync<JsonObject>($"/api/departments?facultyId={engineering}"))!;

        Assert.Equal(2, (int)list["totalCount"]!);
        Assert.Equal(new[] { "CIV", "CS" }, list["items"]!.AsArray().Select(d => (string)d!["code"]!).ToArray());
        Assert.All(list["items"]!.AsArray(), d => Assert.Equal("ENG", (string?)d!["facultyCode"]));
    }

    [Fact]
    public async Task An_unknown_faculty_is_a_400_on_the_facultyId_field()
    {
        var response = await Admin().PostAsJsonAsync("/api/admin/departments",
            new { facultyId = Guid.NewGuid(), code = "CS", name = "Computer Science", nameAr = "علوم الحاسب" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("facultyId", (string?)(await response.Content.ReadFromJsonAsync<JsonObject>())!["details"]![0]!["field"]);
    }

    [Fact]
    public async Task Department_codes_are_unique_across_faculties()
    {
        await CreateDepartment(await CreateFaculty("ENG"), "CS", "Computer Science", "علوم الحاسب");

        var response = await Admin().PostAsJsonAsync("/api/admin/departments",
            new { facultyId = await CreateFaculty("SCI"), code = "cs", name = "Computer Science", nameAr = "علوم الحاسب" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task No_new_departments_under_a_deactivated_faculty()
    {
        var faculty = await CreateFaculty("OLD");
        await Admin().PutAsJsonAsync($"/api/admin/faculties/{faculty}", new { code = "OLD", name = "Old", nameAr = "قديم", isActive = false });

        var response = await Admin().PostAsJsonAsync("/api/admin/departments",
            new { facultyId = faculty, code = "X1", name = "X", nameAr = "س" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("facultyId", (string?)(await response.Content.ReadFromJsonAsync<JsonObject>())!["details"]![0]!["field"]);
    }

    [Fact]
    public async Task A_department_can_move_to_another_active_faculty()
    {
        var engineering = await CreateFaculty("ENG");
        var science = await CreateFaculty("SCI");
        var department = await CreateDepartment(engineering, "CS", "Computer Science", "علوم الحاسب");

        var response = await Admin().PutAsJsonAsync($"/api/admin/departments/{department}",
            new { facultyId = science, code = "CS", name = "Computer Science", nameAr = "علوم الحاسب", description = "moved", isActive = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("SCI", (string?)(await response.Content.ReadFromJsonAsync<JsonObject>())!["facultyCode"]);
    }

    [Fact]
    public async Task Professors_cannot_create_departments()
    {
        var response = await Factory.CreateClientAs("professor").PostAsJsonAsync("/api/admin/departments",
            new { facultyId = Guid.NewGuid(), code = "CS", name = "Computer Science", nameAr = "علوم الحاسب" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private async Task<Guid> CreateFaculty(string code)
    {
        var response = await Admin().PostAsJsonAsync("/api/admin/faculties", new { code, name = code, nameAr = code });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (Guid)(await response.Content.ReadFromJsonAsync<JsonObject>())!["id"]!;
    }

    private async Task<Guid> CreateDepartment(Guid facultyId, string code, string name, string nameAr)
    {
        var response = await Admin().PostAsJsonAsync("/api/admin/departments", new { facultyId, code, name, nameAr });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (Guid)(await response.Content.ReadFromJsonAsync<JsonObject>())!["id"]!;
    }
}
