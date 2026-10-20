using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace NIMI.UniPortal.Modules.IntegrationTests.Users;

/// <summary>Creates faculties / departments through the real API (as an admin) for tests that need them.</summary>
internal static class OrganizationSetup
{
    public static async Task<Guid> CreateFacultyAsync(HttpClient admin, string code)
    {
        var response = await admin.PostAsJsonAsync("/api/admin/faculties", new { code, name = code, nameAr = code });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (Guid)(await response.Content.ReadFromJsonAsync<JsonObject>())!["id"]!;
    }

    public static async Task<Guid> CreateDepartmentAsync(HttpClient admin, Guid facultyId, string code)
    {
        var response = await admin.PostAsJsonAsync("/api/admin/departments", new { facultyId, code, name = code, nameAr = code });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (Guid)(await response.Content.ReadFromJsonAsync<JsonObject>())!["id"]!;
    }
}
