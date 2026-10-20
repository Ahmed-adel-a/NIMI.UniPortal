using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;

namespace NIMI.UniPortal.Modules.IntegrationTests.Users;

/// <summary>User management: search/filter, activate/deactivate, admin password reset (US-A10).</summary>
public sealed class UserManagementTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    private const string Url = "/api/admin/users";

    [Fact]
    public async Task Admins_filter_by_role_department_faculty_and_search()
    {
        var engineering = await OrganizationSetup.CreateFacultyAsync(Admin(), "ENG");
        var commerce = await OrganizationSetup.CreateFacultyAsync(Admin(), "COM");
        var cs = await OrganizationSetup.CreateDepartmentAsync(Admin(), engineering, "CS");
        var acc = await OrganizationSetup.CreateDepartmentAsync(Admin(), commerce, "ACC");
        await CreateStudent("ahmed@cu.edu.eg", "Ahmed Ali", cs);
        await CreateStudent("mona@cu.edu.eg", "Mona Hassan", cs);
        await CreateStudent("omar@cu.edu.eg", "Omar Said", acc);
        await Admin().PostAsJsonAsync(Url, new { role = "professor", fullName = "Dr. Hany", email = "hany@cu.edu.eg", departmentId = cs, academicTitle = "professor" });

        Assert.Equal(3, await Count("?role=student"));
        Assert.Equal(1, await Count("?role=professor"));
        Assert.Equal(3, await Count($"?departmentId={cs}"));          // 2 students + the professor
        Assert.Equal(1, await Count($"?facultyId={commerce}"));
        Assert.Equal(1, await Count("?search=mona"));                 // by name
        Assert.Equal(1, await Count("?search=CS-2026-0002"));         // by student code
        Assert.Equal(1, await Count("?search=OMAR@CU"));              // by email, any case

        var page = (await Admin().GetFromJsonAsync<JsonObject>($"{Url}?pageSize=2&page=2"))!;
        Assert.Equal(4, (int)page["totalCount"]!);
        Assert.Equal(2, page["items"]!.AsArray().Count);
    }

    [Theory]
    [InlineData("teacher")]
    [InlineData("1")]
    public async Task An_unknown_role_filter_is_a_400_on_role(string role)
    {
        var response = await Admin().GetAsync($"{Url}?role={role}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("role", (string?)(await response.Content.ReadFromJsonAsync<JsonObject>())!["details"]![0]!["field"]);
    }

    [Fact]
    public async Task A_deactivated_user_cannot_log_in_until_reactivated()
    {
        var cs = await Department();
        var (id, password) = await CreateStudent("ahmed@cu.edu.eg", "Ahmed Ali", cs);

        Assert.Equal(HttpStatusCode.NoContent, (await Admin().PatchAsJsonAsync($"{Url}/{id}/status", new { isActive = false })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await TryLogin("ahmed@cu.edu.eg", password)).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await Admin().PatchAsJsonAsync($"{Url}/{id}/status", new { isActive = true })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await TryLogin("ahmed@cu.edu.eg", password)).StatusCode);
    }

    [Fact]
    public async Task Admins_cannot_deactivate_themselves()
    {
        var self = TestAuthHandler.UserIdFor("admin");

        var response = await Admin().PatchAsJsonAsync($"{Url}/{self}/status", new { isActive = false });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("isActive", (string?)(await response.Content.ReadFromJsonAsync<JsonObject>())!["details"]![0]!["field"]);
    }

    [Fact]
    public async Task A_reset_gives_a_new_temporary_password_unlocks_and_forces_a_change()
    {
        var cs = await Department();
        var (id, oldPassword) = await CreateStudent("ahmed@cu.edu.eg", "Ahmed Ali", cs);
        for (var attempt = 1; attempt <= 5; attempt++)
            await TryLogin("ahmed@cu.edu.eg", "wrong-123");   // locked now

        var reset = await Admin().PostAsync($"{Url}/{id}/reset-password", null);

        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
        var temporaryPassword = (string)(await reset.Content.ReadFromJsonAsync<JsonObject>())!["temporaryPassword"]!;
        Assert.Equal(HttpStatusCode.Unauthorized, (await TryLogin("ahmed@cu.edu.eg", oldPassword)).StatusCode);
        var login = await Factory.LoginAsync("ahmed@cu.edu.eg", temporaryPassword);   // lock cleared
        Assert.True((bool)login["mustChangePassword"]!);
    }

    [Fact]
    public async Task Unknown_users_are_a_404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await Admin().PostAsync($"{Url}/{Guid.NewGuid()}/reset-password", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Admin().PatchAsJsonAsync($"{Url}/{Guid.NewGuid()}/status", new { isActive = true })).StatusCode);
    }

    [Fact]
    public async Task Students_and_professors_cannot_manage_accounts()
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await Factory.CreateClientAs("student").GetAsync(Url)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Factory.CreateClientAs("professor").PostAsync($"{Url}/{Guid.NewGuid()}/reset-password", null)).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private async Task<Guid> Department() =>
        await OrganizationSetup.CreateDepartmentAsync(Admin(), await OrganizationSetup.CreateFacultyAsync(Admin(), "ENG"), "CS");

    private async Task<(Guid Id, string Password)> CreateStudent(string email, string fullName, Guid departmentId)
    {
        var response = await Admin().PostAsJsonAsync(Url, new { role = "student", fullName, email, departmentId, enrollmentYear = 2026 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
        return ((Guid)body["user"]!["id"]!, (string)body["temporaryPassword"]!);
    }

    private async Task<int> Count(string query) =>
        (int)(await Admin().GetFromJsonAsync<JsonObject>(Url + query))!["totalCount"]!;

    private Task<HttpResponseMessage> TryLogin(string email, string password) =>
        Factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email, password });
}
