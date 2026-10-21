// ACCEPTANCE TEST — written by the consultant for phase P1.4. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Users;

namespace NIMI.UniPortal.Modules.IntegrationTests.Platform;

/// <summary>P1.4 — per-faculty overrides of the academic rules + effective settings.</summary>
public sealed class FacultySettingsTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task A_faculty_without_overrides_inherits_every_university_rule()
    {
        var eng = await OrganizationSetup.CreateFacultyAsync(Admin(), "ENG");

        var response = await Admin().GetAsync(OverridesUrl(eng));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
        Assert.Equal(eng, (Guid)dto["facultyId"]!);
        Assert.Null(dto["version"]);
        var overrides = dto["overrides"]!.AsObject();
        Assert.All(OverrideFields, field => Assert.Null(overrides[field]));

        var effective = await Effective(eng);
        Assert.Equal(eng, (Guid)effective["facultyId"]!);
        Assert.Equal("Africa/Cairo", (string?)effective["timeZone"]);
        Assert.Equal(0, (int)effective["weekStartDay"]!);
        Assert.Equal(new[] { 0, 1, 2, 3, 4, 5 }, Ints(effective["workingDays"]));
        Assert.Equal(75m, (decimal)effective["attendanceThreshold"]!);
        Assert.Equal(10, (int)effective["lateGraceMinutes"]!);
        Assert.Equal(12, effective["gradingScale"]!.AsArray().Count);
        Assert.Equal("{DEPT}-{YEAR}-{SEQ:0000}", (string?)effective["studentCodeFormat"]);
        Assert.Equal("opt_in", (string?)effective["militaryEducationPolicy"]!["mode"]);
    }

    [Fact]
    public async Task Effective_settings_without_a_faculty_are_the_university_defaults()
    {
        var effective = (await Factory.CreateClientAs("student").GetFromJsonAsync<JsonObject>("/api/settings/effective"))!;

        Assert.Null(effective["facultyId"]);
        Assert.Equal(75m, (decimal)effective["attendanceThreshold"]!);
        Assert.Equal("{DEPT}-{YEAR}-{SEQ:0000}", (string?)effective["studentCodeFormat"]);
    }

    [Fact]
    public async Task Overriding_one_rule_changes_only_that_rule_for_that_faculty()
    {
        var com = await OrganizationSetup.CreateFacultyAsync(Admin(), "COM");

        var response = await Admin().PutAsJsonAsync(OverridesUrl(com), new { attendanceThreshold = 60 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
        Assert.Equal(60m, (decimal)dto["overrides"]!["attendanceThreshold"]!);
        Assert.Null(dto["overrides"]!["lateGraceMinutes"]);
        Assert.Equal(60m, (decimal)dto["effective"]!["attendanceThreshold"]!);
        Assert.NotNull(dto["version"]);
        Assert.NotNull((string?)dto["updatedAt"]);

        var effective = await Effective(com);
        Assert.Equal(60m, (decimal)effective["attendanceThreshold"]!);
        Assert.Equal(10, (int)effective["lateGraceMinutes"]!);                                  // still inherited
        Assert.Equal(new[] { 0, 1, 2, 3, 4, 5 }, Ints(effective["workingDays"]));
        Assert.Equal(75m, (decimal)(await AdminSettings())["attendanceThreshold"]!);            // university untouched
    }

    [Fact]
    public async Task Every_overridable_rule_can_be_overridden()
    {
        var med = await OrganizationSetup.CreateFacultyAsync(Admin(), "MED");

        var response = await Admin().PutAsJsonAsync(OverridesUrl(med), new
        {
            weekStartDay = 1,
            workingDays = new[] { 1, 2, 3, 4, 5 },
            attendanceThreshold = 85,
            lateGraceMinutes = 5,
            gradingScale = new[]
            {
                new { grade = "ناجح", minScore = 50, points = 1 },
                new { grade = "راسب", minScore = 0, points = 0 },
            },
            studentCodeFormat = "{FAC}{YEAR}{SEQ:000}",
            militaryEducationPolicy = new { mode = "mandatory" },
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var effective = await Effective(med);
        Assert.Equal(1, (int)effective["weekStartDay"]!);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, Ints(effective["workingDays"]));
        Assert.Equal(85m, (decimal)effective["attendanceThreshold"]!);
        Assert.Equal(5, (int)effective["lateGraceMinutes"]!);
        var scale = effective["gradingScale"]!.AsArray();
        Assert.Equal(2, scale.Count);
        Assert.Contains(scale, band => (string?)band!["grade"] == "ناجح" && (decimal)band["minScore"]! == 50m);   // Arabic round-trip
        Assert.Equal("{FAC}{YEAR}{SEQ:000}", (string?)effective["studentCodeFormat"]);
        Assert.Equal("mandatory", (string?)effective["militaryEducationPolicy"]!["mode"]);
        Assert.Equal("Africa/Cairo", (string?)effective["timeZone"]);                           // never overridable
    }

    [Fact]
    public async Task Overrides_of_one_faculty_never_affect_another()
    {
        var com = await OrganizationSetup.CreateFacultyAsync(Admin(), "COM");
        var eng = await OrganizationSetup.CreateFacultyAsync(Admin(), "ENG");

        Assert.Equal(HttpStatusCode.OK, (await Admin().PutAsJsonAsync(OverridesUrl(com), new { attendanceThreshold = 60, lateGraceMinutes = 20 })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Admin().PutAsJsonAsync(OverridesUrl(eng), new { attendanceThreshold = 80 })).StatusCode);

        var comEffective = await Effective(com);
        var engEffective = await Effective(eng);
        Assert.Equal(60m, (decimal)comEffective["attendanceThreshold"]!);
        Assert.Equal(20, (int)comEffective["lateGraceMinutes"]!);
        Assert.Equal(80m, (decimal)engEffective["attendanceThreshold"]!);
        Assert.Equal(10, (int)engEffective["lateGraceMinutes"]!);
    }

    [Fact]
    public async Task A_university_default_change_flows_through_to_faculties_that_inherit_it()
    {
        var eng = await OrganizationSetup.CreateFacultyAsync(Admin(), "ENG");
        Assert.Equal(HttpStatusCode.OK, (await Admin().PutAsJsonAsync(OverridesUrl(eng), new { attendanceThreshold = 80 })).StatusCode);
        var settings = await AdminSettings();
        settings["lateGraceMinutes"] = 25;
        settings["attendanceThreshold"] = 70;
        Assert.Equal(HttpStatusCode.OK, (await Admin().PutAsJsonAsync("/api/admin/settings", settings)).StatusCode);

        var effective = await Effective(eng);

        Assert.Equal(25, (int)effective["lateGraceMinutes"]!);                                  // inherited → follows the default
        Assert.Equal(80m, (decimal)effective["attendanceThreshold"]!);                          // overridden → keeps its value
    }

    [Theory]
    [InlineData("""{ "attendanceThreshold": 120 }""", "attendanceThreshold")]
    [InlineData("""{ "lateGraceMinutes": 500 }""", "lateGraceMinutes")]
    [InlineData("""{ "weekStartDay": 7 }""", "weekStartDay")]
    [InlineData("""{ "workingDays": [] }""", "workingDays")]
    [InlineData("""{ "workingDays": [1, 1] }""", "workingDays")]
    [InlineData("""{ "studentCodeFormat": "{DEPT}-{YEAR}" }""", "studentCodeFormat")]
    [InlineData("""{ "gradingScale": [{ "grade": "A", "minScore": 90, "points": 4 }] }""", "gradingScale")]
    public async Task Invalid_override_values_are_a_400_naming_the_field(string body, string field)
    {
        var eng = await OrganizationSetup.CreateFacultyAsync(Admin(), "ENG");

        var response = await Admin().PutAsJsonAsync(OverridesUrl(eng), JsonNode.Parse(body));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
        Assert.Contains(field, error["details"]!.AsArray().Select(d => (string?)d!["field"]));
        Assert.Null((await Admin().GetFromJsonAsync<JsonObject>(OverridesUrl(eng)))!["version"]);   // nothing was saved
    }

    [Fact]
    public async Task An_unknown_faculty_is_a_404()
    {
        var unknown = OverridesUrl(Guid.CreateVersion7());

        var get = await Admin().GetAsync(unknown);
        var put = await Admin().PutAsJsonAsync(unknown, new { attendanceThreshold = 60 });
        var delete = await Admin().DeleteAsync(unknown);

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal("not_found", (string?)(await get.Content.ReadFromJsonAsync<JsonObject>())!["code"]);
        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
    }

    [Fact]
    public async Task Saving_with_a_stale_version_is_a_409()
    {
        var eng = await OrganizationSetup.CreateFacultyAsync(Admin(), "ENG");
        Assert.Equal(HttpStatusCode.OK, (await Admin().PutAsJsonAsync(OverridesUrl(eng), new { attendanceThreshold = 70 })).StatusCode);
        var loadedByAdminA = (await Admin().GetFromJsonAsync<JsonObject>(OverridesUrl(eng)))!;
        var loadedByAdminB = (await Admin().GetFromJsonAsync<JsonObject>(OverridesUrl(eng)))!;

        var savedByA = await Admin().PutAsJsonAsync(OverridesUrl(eng), new { attendanceThreshold = 65, version = (uint)loadedByAdminA["version"]! });
        var savedByB = await Admin().PutAsJsonAsync(OverridesUrl(eng), new { attendanceThreshold = 90, version = (uint)loadedByAdminB["version"]! });

        Assert.Equal(HttpStatusCode.OK, savedByA.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, savedByB.StatusCode);
        Assert.Equal("conflict", (string?)(await savedByB.Content.ReadFromJsonAsync<JsonObject>())!["code"]);
        Assert.Equal(65m, (decimal)(await Effective(eng))["attendanceThreshold"]!);
    }

    [Fact]
    public async Task Clearing_every_override_returns_the_faculty_to_the_defaults()
    {
        var com = await OrganizationSetup.CreateFacultyAsync(Admin(), "COM");
        var eng = await OrganizationSetup.CreateFacultyAsync(Admin(), "ENG");
        var comSaved = await PutOverrides(com, new { attendanceThreshold = 60 });
        await PutOverrides(eng, new { lateGraceMinutes = 30 });

        var clearedByPut = await Admin().PutAsJsonAsync(OverridesUrl(com), new { version = (uint)comSaved["version"]! });   // every field null
        var clearedByDelete = await Admin().DeleteAsync(OverridesUrl(eng));
        var deletedAgain = await Admin().DeleteAsync(OverridesUrl(eng));

        Assert.Equal(HttpStatusCode.OK, clearedByPut.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, clearedByDelete.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deletedAgain.StatusCode);                         // idempotent
        foreach (var faculty in new[] { com, eng })
        {
            var dto = (await Admin().GetFromJsonAsync<JsonObject>(OverridesUrl(faculty)))!;
            Assert.Null(dto["version"]);
            Assert.All(OverrideFields, field => Assert.Null(dto["overrides"]![field]));
            var effective = await Effective(faculty);
            Assert.Equal(75m, (decimal)effective["attendanceThreshold"]!);
            Assert.Equal(10, (int)effective["lateGraceMinutes"]!);
        }
    }

    [Fact]
    public async Task Only_admins_manage_overrides_but_any_signed_in_user_reads_effective_settings()
    {
        var eng = await OrganizationSetup.CreateFacultyAsync(Admin(), "ENG");
        var url = OverridesUrl(eng);

        foreach (var role in new[] { "professor", "student" })
        {
            var client = Factory.CreateClientAs(role);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(url)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync(url, new { attendanceThreshold = 1 })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync(url)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(EffectiveUrl(eng))).StatusCode);
        }

        var anonymous = Factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PutAsJsonAsync(url, new { attendanceThreshold = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.DeleteAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(EffectiveUrl(eng))).StatusCode);
    }

    [Fact]
    public async Task Students_of_a_faculty_get_codes_in_that_facultys_format()
    {
        var com = await OrganizationSetup.CreateFacultyAsync(Admin(), "COM");
        var eng = await OrganizationSetup.CreateFacultyAsync(Admin(), "ENG");
        var acc = await OrganizationSetup.CreateDepartmentAsync(Admin(), com, "ACC");
        var cs = await OrganizationSetup.CreateDepartmentAsync(Admin(), eng, "CS");
        var beforeOverride = await CreateStudentCode("first@cu.edu.eg", acc);

        await PutOverrides(com, new { studentCodeFormat = "{FAC}{YEAR}{SEQ:000}" });
        var commerceStudent = await CreateStudentCode("second@cu.edu.eg", acc);
        var engineeringStudent = await CreateStudentCode("third@cu.edu.eg", cs);

        Assert.Equal("ACC-2026-0001", beforeOverride);                                          // issued before: university format
        Assert.Equal("COM2026001", commerceStudent);                                            // new prefix → its own counter
        Assert.Equal("CS-2026-0001", engineeringStudent);                                       // other faculty: still the default
    }

    private static readonly string[] OverrideFields =
    [
        "weekStartDay", "workingDays", "attendanceThreshold", "lateGraceMinutes",
        "gradingScale", "studentCodeFormat", "militaryEducationPolicy",
    ];

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private static string OverridesUrl(Guid facultyId) => $"/api/admin/faculties/{facultyId}/settings";

    private static string EffectiveUrl(Guid facultyId) => $"/api/settings/effective?facultyId={facultyId}";

    private async Task<JsonObject> Effective(Guid facultyId) =>
        (await Factory.CreateClientAs("student").GetFromJsonAsync<JsonObject>(EffectiveUrl(facultyId)))!;

    private async Task<JsonObject> AdminSettings() => (await Admin().GetFromJsonAsync<JsonObject>("/api/admin/settings"))!;

    private async Task<JsonObject> PutOverrides(Guid facultyId, object body)
    {
        var response = await Admin().PutAsJsonAsync(OverridesUrl(facultyId), body);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonObject>())!;
    }

    private async Task<string?> CreateStudentCode(string email, Guid departmentId)
    {
        var response = await Admin().PostAsJsonAsync("/api/admin/users", new
        {
            role = "student", fullName = "طالب " + email, email, departmentId, enrollmentYear = 2026, gender = "male",
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (string?)(await response.Content.ReadFromJsonAsync<JsonObject>())!["user"]!["studentCode"];
    }

    private static int[] Ints(JsonNode? array) => array!.AsArray().Select(n => (int)n!).ToArray();
}
