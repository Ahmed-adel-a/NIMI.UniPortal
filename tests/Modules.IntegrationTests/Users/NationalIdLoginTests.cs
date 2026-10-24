// ACCEPTANCE TEST — written by the consultant for decision NATIONALIDLOGIN. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;

namespace NIMI.UniPortal.Modules.IntegrationTests.Users;

/// <summary>
/// Decision NATIONALIDLOGIN: the login field takes an email or a national ID. When one national ID has several accounts
/// whose password matches, the API asks which role to use (409 <c>choice_required</c>, one detail per role) and the
/// client sends the login again with <c>role</c>.
/// </summary>
public sealed class NationalIdLoginTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    private const string NationalId = "29901011234567";
    private const string Password = "Portal!Pass2026";

    [Fact]
    public async Task A_national_id_with_one_account_signs_in_directly()
    {
        var admin = Factory.CreateClientAs("admin");
        await CreateAsync(admin, "student", "ahmed@cu.edu.eg", await DepartmentAsync(admin));

        var login = await LoginAsync(NationalId, Password);

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.NotNull((string?)(await login.Content.ReadFromJsonAsync<JsonObject>())!["accessToken"]);
    }

    [Fact]
    public async Task Several_accounts_for_one_national_id_ask_for_the_role_then_sign_in_with_it()
    {
        var admin = Factory.CreateClientAs("admin");
        var department = await DepartmentAsync(admin);
        await CreateAsync(admin, "student", "student@cu.edu.eg", department);
        await CreateAsync(admin, "professor", "professor@cu.edu.eg", department);

        var ask = await LoginAsync(NationalId, Password);
        Assert.Equal(HttpStatusCode.Conflict, ask.StatusCode);
        var error = (await ask.Content.ReadFromJsonAsync<JsonObject>())!;
        Assert.Equal("choice_required", (string?)error["code"]);
        Assert.Equal(
            new[] { "professor", "student" },
            error["details"]!.AsArray().Select(d => (string)d!["error"]!).Order());
        Assert.All(error["details"]!.AsArray(), d => Assert.Equal("role", (string?)d!["field"]));

        var chosen = await LoginAsync(NationalId, Password, role: "professor");
        Assert.Equal(HttpStatusCode.OK, chosen.StatusCode);
        var token = (string)(await chosen.Content.ReadFromJsonAsync<JsonObject>())!["accessToken"]!;
        var me = (await Factory.CreateClientWithToken(token).GetFromJsonAsync<JsonObject>("/api/auth/me"))!;
        Assert.Equal("professor", (string?)me["role"]);
    }

    [Fact]
    public async Task Wrong_passwords_and_roles_the_person_does_not_have_fail_like_any_bad_login()
    {
        var admin = Factory.CreateClientAs("admin");
        await CreateAsync(admin, "student", "ahmed@cu.edu.eg", await DepartmentAsync(admin));

        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(NationalId, "Wrong!Pass2026")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(NationalId, Password, role: "admin")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync("30000000000000", Password)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await LoginAsync(NationalId, Password, role: "wizard")).StatusCode);
    }

    [Fact]
    public async Task Email_login_is_unchanged()
    {
        var admin = Factory.CreateClientAs("admin");
        await CreateAsync(admin, "student", "ahmed@cu.edu.eg", await DepartmentAsync(admin));

        Assert.Equal(HttpStatusCode.OK, (await LoginAsync("Ahmed@CU.edu.eg", Password)).StatusCode);
    }

    private Task<HttpResponseMessage> LoginAsync(string login, string password, string? role = null) =>
        Factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email = login, password, role });

    private static async Task CreateAsync(HttpClient admin, string role, string email, Guid departmentId)
    {
        var created = await admin.PostAsJsonAsync("/api/admin/users", role == "student"
            ? new { role, fullName = "Ahmed Ali", email, password = Password, departmentId = (Guid?)departmentId, enrollmentYear = (int?)2026, academicTitle = (string?)null, nationalId = NationalId }
            : new { role, fullName = "Ahmed Ali", email, password = Password, departmentId = (Guid?)departmentId, enrollmentYear = (int?)null, academicTitle = (string?)"lecturer", nationalId = NationalId });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
    }

    private static async Task<Guid> DepartmentAsync(HttpClient admin)
    {
        var faculty = await OrganizationSetup.CreateFacultyAsync(admin, "ENG");
        return await OrganizationSetup.CreateDepartmentAsync(admin, faculty, "CS");
    }
}
