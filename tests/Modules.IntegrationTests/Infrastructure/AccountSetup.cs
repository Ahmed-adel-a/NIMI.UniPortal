using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;

/// <summary>
/// Creates a student / professor through the admin API, logs in with the temporary password and replaces it,
/// then returns a real token that passes the default policy (no pending password change). P2 test infrastructure;
/// later phases reuse it.
/// </summary>
internal static class AccountSetup
{
    private const string NewPassword = "Portal!Pass2026";

    public static Task<(Guid Id, string Token)> CreateStudentWithTokenAsync(
        IPortalHost factory, HttpClient admin, Guid departmentId, string email) =>
        CreateWithTokenAsync(factory, admin, email, new
        {
            role = "student",
            fullName = "Test Student",
            email,
            departmentId,
            enrollmentYear = 2026,
            gender = "male",
        });

    public static Task<(Guid Id, string Token)> CreateProfessorWithTokenAsync(
        IPortalHost factory, HttpClient admin, Guid departmentId, string email) =>
        CreateWithTokenAsync(factory, admin, email, new
        {
            role = "professor",
            fullName = "Test Professor",
            email,
            departmentId,
            academicTitle = "professor",
        });

    /// <summary>Decision ROLES2: a department head is its own kind of account, one active per department.</summary>
    public static Task<(Guid Id, string Token)> CreateDepartmentHeadWithTokenAsync(
        IPortalHost factory, HttpClient admin, Guid departmentId, string email) =>
        CreateWithTokenAsync(factory, admin, email, new
        {
            role = "department_head",
            fullName = "Test Department Head",
            email,
            departmentId,
        });

    private static async Task<(Guid Id, string Token)> CreateWithTokenAsync(
        IPortalHost factory, HttpClient admin, string email, object body)
    {
        var created = await admin.PostAsJsonAsync("/api/admin/users", body);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var payload = (await created.Content.ReadFromJsonAsync<JsonObject>())!;
        var id = (Guid)payload["user"]!["id"]!;
        var temporaryPassword = (string)payload["temporaryPassword"]!;

        var login = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email, password = temporaryPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var temporaryToken = (string)(await login.Content.ReadFromJsonAsync<JsonObject>())!["accessToken"]!;

        var change = await factory.CreateClientWithToken(temporaryToken)
            .PostAsJsonAsync("/api/auth/change-password", new { currentPassword = temporaryPassword, newPassword = NewPassword });
        Assert.Equal(HttpStatusCode.OK, change.StatusCode);
        var token = (string)(await change.Content.ReadFromJsonAsync<JsonObject>())!["accessToken"]!;

        return (id, token);
    }
}
