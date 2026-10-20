using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;

namespace NIMI.UniPortal.Modules.IntegrationTests.Users;

/// <summary>Create student / professor accounts with generated student codes (US-A01 single, US-A10).</summary>
public sealed class CreateUserTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    private const string Url = "/api/admin/users";

    [Fact]
    public async Task Students_get_sequential_codes_per_department_and_year()
    {
        var engineering = await OrganizationSetup.CreateFacultyAsync(Admin(), "ENG");
        var cs = await OrganizationSetup.CreateDepartmentAsync(Admin(), engineering, "CS");
        var civil = await OrganizationSetup.CreateDepartmentAsync(Admin(), engineering, "CIV");

        var first = await CreateStudent("ahmed@cu.edu.eg", cs, 2026);
        var second = await CreateStudent("mona@cu.edu.eg", cs, 2026);
        var otherDepartment = await CreateStudent("omar@cu.edu.eg", civil, 2026);
        var otherYear = await CreateStudent("sara@cu.edu.eg", cs, 2025);

        Assert.Equal("CS-2026-0001", (string?)first["user"]!["studentCode"]);
        Assert.Equal("CS-2026-0002", (string?)second["user"]!["studentCode"]);
        Assert.Equal("CIV-2026-0001", (string?)otherDepartment["user"]!["studentCode"]);   // each prefix counts on its own
        Assert.Equal("CS-2025-0001", (string?)otherYear["user"]!["studentCode"]);
        Assert.Equal("ENG", (string?)first["user"]!["facultyCode"]);
    }

    [Fact]
    public async Task A_new_student_gets_a_one_time_temporary_password_and_must_change_it()
    {
        var cs = await Department("CS");

        var created = await CreateStudent("ahmed@cu.edu.eg", cs, 2026);
        var temporaryPassword = (string)created["temporaryPassword"]!;

        Assert.Equal(10, temporaryPassword.Length);
        Assert.True((bool)created["user"]!["mustChangePassword"]!);
        var login = await Factory.LoginAsync("ahmed@cu.edu.eg", temporaryPassword);
        Assert.True((bool)login["mustChangePassword"]!);
    }

    [Fact]
    public async Task A_password_chosen_by_the_admin_is_used_and_not_echoed_back()
    {
        var cs = await Department("CS");

        var response = await Admin().PostAsJsonAsync(Url, new
        {
            role = "student", fullName = "Ahmed Ali", email = "ahmed@cu.edu.eg", password = "Chosen123",
            departmentId = cs, enrollmentYear = 2026,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Null((string?)(await response.Content.ReadFromJsonAsync<JsonObject>())!["temporaryPassword"]);
        Assert.True((bool)(await Factory.LoginAsync("ahmed@cu.edu.eg", "Chosen123"))["mustChangePassword"]!);   // still forced
    }

    [Fact]
    public async Task A_professor_is_created_with_an_academic_title_and_no_student_code()
    {
        var cs = await Department("CS");

        var response = await Admin().PostAsJsonAsync(Url, new
        {
            role = "professor", fullName = "Dr. Hany", email = "hany@cu.edu.eg", departmentId = cs, academicTitle = "demonstrator",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = (await response.Content.ReadFromJsonAsync<JsonObject>())!["user"]!;
        Assert.Equal("professor", (string?)user["role"]);
        Assert.Equal("demonstrator", (string?)user["academicTitle"]);
        Assert.Null((string?)user["studentCode"]);
    }

    [Fact]
    public async Task Role_specific_fields_are_required_and_named_in_the_400()
    {
        var studentWithoutYear = await Admin().PostAsJsonAsync(Url, new { role = "student", fullName = "A", email = "a@cu.edu.eg", departmentId = Guid.NewGuid() });
        var professorWithoutTitle = await Admin().PostAsJsonAsync(Url, new { role = "professor", fullName = "B", email = "b@cu.edu.eg", departmentId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.BadRequest, studentWithoutYear.StatusCode);
        Assert.Contains("enrollmentYear", await Fields(studentWithoutYear));
        Assert.Equal(HttpStatusCode.BadRequest, professorWithoutTitle.StatusCode);
        Assert.Contains("academicTitle", await Fields(professorWithoutTitle));
    }

    [Fact]
    public async Task A_duplicate_email_is_a_409_regardless_of_case()
    {
        var cs = await Department("CS");
        await CreateStudent("ahmed@cu.edu.eg", cs, 2026);

        var response = await Admin().PostAsJsonAsync(Url, new { role = "student", fullName = "Twin", email = "AHMED@cu.edu.eg", departmentId = cs, enrollmentYear = 2026 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_or_deactivated_departments_are_a_400_on_departmentId()
    {
        var engineering = await OrganizationSetup.CreateFacultyAsync(Admin(), "ENG");
        var closed = await OrganizationSetup.CreateDepartmentAsync(Admin(), engineering, "OLD");
        await Admin().PutAsJsonAsync($"/api/admin/departments/{closed}", new { facultyId = engineering, code = "OLD", name = "Old", nameAr = "قديم", isActive = false });

        var unknown = await Admin().PostAsJsonAsync(Url, new { role = "student", fullName = "A", email = "a@cu.edu.eg", departmentId = Guid.NewGuid(), enrollmentYear = 2026 });
        var deactivated = await Admin().PostAsJsonAsync(Url, new { role = "student", fullName = "B", email = "b@cu.edu.eg", departmentId = closed, enrollmentYear = 2026 });

        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Contains("departmentId", await Fields(unknown));
        Assert.Equal(HttpStatusCode.BadRequest, deactivated.StatusCode);
        Assert.Contains("departmentId", await Fields(deactivated));
    }

    [Fact]
    public async Task The_code_follows_the_format_in_the_settings()
    {
        var cs = await Department("CS");
        var settings = (await Admin().GetFromJsonAsync<JsonObject>("/api/admin/settings"))!;
        settings["studentCodeFormat"] = "{FAC}{YEAR}{SEQ:000}";
        Assert.Equal(HttpStatusCode.OK, (await Admin().PutAsJsonAsync("/api/admin/settings", settings)).StatusCode);

        var created = await CreateStudent("ahmed@cu.edu.eg", cs, 2026);

        Assert.Equal("ENG2026001", (string?)created["user"]!["studentCode"]);
    }

    [Fact]
    public async Task Only_admins_create_accounts()
    {
        var body = new { role = "student", fullName = "A", email = "a@cu.edu.eg", departmentId = Guid.NewGuid(), enrollmentYear = 2026 };

        Assert.Equal(HttpStatusCode.Forbidden, (await Factory.CreateClientAs("professor").PostAsJsonAsync(Url, body)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Factory.CreateClient().PostAsJsonAsync(Url, body)).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private async Task<Guid> Department(string code) =>
        await OrganizationSetup.CreateDepartmentAsync(Admin(), await OrganizationSetup.CreateFacultyAsync(Admin(), "ENG"), code);

    private async Task<JsonObject> CreateStudent(string email, Guid departmentId, int enrollmentYear)
    {
        var response = await Admin().PostAsJsonAsync(Url, new
        {
            role = "student", fullName = "Student " + email, email, departmentId, enrollmentYear, gender = "male",
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonObject>())!;
    }

    private static async Task<List<string?>> Fields(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonObject>())!["details"]!.AsArray().Select(d => (string?)d!["field"]).ToList();
}
