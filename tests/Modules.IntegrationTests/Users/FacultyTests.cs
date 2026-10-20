using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;

namespace NIMI.UniPortal.Modules.IntegrationTests.Users;

/// <summary>Faculties (US-A16): Student Affairs manages them; every signed-in user can list them.</summary>
public sealed class FacultyTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task Admin_creates_a_faculty_and_everyone_signed_in_can_list_it()
    {
        var created = await CreateFaculty("eng", "Engineering", "الهندسة");

        Assert.Equal("ENG", (string?)created["code"]);   // codes are stored upper-case
        Assert.Equal("الهندسة", (string?)created["nameAr"]);

        var list = (await Factory.CreateClientAs("student").GetFromJsonAsync<JsonObject>("/api/faculties"))!;
        Assert.Equal(1, (int)list["totalCount"]!);
        Assert.Equal("ENG", (string?)list["items"]![0]!["code"]);
    }

    [Fact]
    public async Task A_duplicate_code_is_a_409_conflict()
    {
        await CreateFaculty("ENG", "Engineering", "الهندسة");

        var response = await Admin().PostAsJsonAsync("/api/admin/faculties", new { code = "eng", name = "Other", nameAr = "أخرى" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_codes_and_missing_names_are_a_400_naming_the_fields()
    {
        var response = await Admin().PostAsJsonAsync("/api/admin/faculties", new { code = "E", name = "", nameAr = "الهندسة" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var fields = (await response.Content.ReadFromJsonAsync<JsonObject>())!["details"]!.AsArray().Select(d => (string?)d!["field"]).ToList();
        Assert.Contains("code", fields);
        Assert.Contains("name", fields);
    }

    [Fact]
    public async Task Only_admins_manage_faculties_and_anonymous_callers_cannot_even_list()
    {
        var body = new { code = "ENG", name = "Engineering", nameAr = "الهندسة" };

        Assert.Equal(HttpStatusCode.Forbidden, (await Factory.CreateClientAs("professor").PostAsJsonAsync("/api/admin/faculties", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Factory.CreateClient().GetAsync("/api/faculties")).StatusCode);
    }

    [Fact]
    public async Task A_deactivated_faculty_is_hidden_unless_asked_for()
    {
        var faculty = await CreateFaculty("COM", "Commerce", "التجارة");

        var update = await Admin().PutAsJsonAsync($"/api/admin/faculties/{faculty["id"]}",
            new { code = "COM", name = "Commerce", nameAr = "التجارة", isActive = false });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var active = (await Admin().GetFromJsonAsync<JsonObject>("/api/faculties"))!;
        var all = (await Admin().GetFromJsonAsync<JsonObject>("/api/faculties?includeInactive=true"))!;
        Assert.Equal(0, (int)active["totalCount"]!);
        Assert.Equal(1, (int)all["totalCount"]!);
    }

    [Fact]
    public async Task Updating_an_unknown_faculty_is_a_404()
    {
        var response = await Admin().PutAsJsonAsync($"/api/admin/faculties/{Guid.NewGuid()}",
            new { code = "XX", name = "X", nameAr = "س", isActive = true });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Lists_are_paged()
    {
        foreach (var code in new[] { "AA", "BB", "CC" })
            await CreateFaculty(code, code, code);

        var page = (await Admin().GetFromJsonAsync<JsonObject>("/api/faculties?page=2&pageSize=2"))!;

        Assert.Equal(3, (int)page["totalCount"]!);
        Assert.Equal(2, (int)page["totalPages"]!);
        Assert.Equal("CC", (string?)page["items"]!.AsArray().Single()!["code"]);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private async Task<JsonObject> CreateFaculty(string code, string name, string nameAr)
    {
        var response = await Admin().PostAsJsonAsync("/api/admin/faculties", new { code, name, nameAr });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonObject>())!;
    }
}
