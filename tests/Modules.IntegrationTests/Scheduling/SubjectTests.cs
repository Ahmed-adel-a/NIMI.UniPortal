// ACCEPTANCE TEST — written by the consultant for phase P03. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Users;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Scheduling;

/// <summary>P3.2 — the subject catalog (rule R3).</summary>
public sealed class SubjectTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task Admin_creates_a_subject_and_the_code_is_upper_cased()
    {
        var (_, cs, _) = await Organization();

        var response = await PostSubjectAsync(Admin(), cs, "cs301", "Algorithms", "الخوارزميات", creditHours: 3);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var subject = await BodyOf(response);
        Assert.Equal("CS301", (string?)subject["code"]);
        var read = (await Student().GetFromJsonAsync<JsonObject>($"/api/subjects/{(Guid)subject["id"]!}"))!;
        Assert.Equal(cs, (Guid)read["departmentId"]!);
        Assert.Equal("الخوارزميات", (string?)read["nameAr"]);
        Assert.Equal(3, (int)read["creditHours"]!);
        Assert.Equal("regular", (string?)read["category"]);
        Assert.False((bool)read["isGraduationRequirement"]!);
        Assert.True((bool)read["isActive"]!);
    }

    [Fact]
    public async Task A_code_used_twice_is_a_409_whatever_the_case()
    {
        var (_, cs, _) = await Organization();
        await CreateSubjectAsync(Admin(), cs, "CS301");

        var duplicate = await PostSubjectAsync(Admin(), cs, "cs301");

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task An_unknown_department_is_a_400_on_departmentId()
    {
        var response = await PostSubjectAsync(Admin(), Guid.CreateVersion7(), "CS301");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("departmentId", await FieldsOf(response));
    }

    [Fact]
    public async Task Invalid_values_are_a_400_naming_the_field()
    {
        var (_, cs, _) = await Organization();

        var tooManyCredits = await PostSubjectAsync(Admin(), cs, "CS301", creditHours: 13);
        var badCode = await PostSubjectAsync(Admin(), cs, "C S");

        Assert.Equal(HttpStatusCode.BadRequest, tooManyCredits.StatusCode);
        Assert.Contains("creditHours", await FieldsOf(tooManyCredits));
        Assert.Equal(HttpStatusCode.BadRequest, badCode.StatusCode);
        Assert.Contains("code", await FieldsOf(badCode));
    }

    [Fact]
    public async Task Subjects_are_searchable_by_code_or_arabic_name_and_filterable_by_department()
    {
        var (_, cs, civ) = await Organization();
        await CreateSubjectAsync(Admin(), cs, "CS302", "Databases", "قواعد البيانات");
        await CreateSubjectAsync(Admin(), cs, "CS301", "Algorithms", "الخوارزميات");
        await CreateSubjectAsync(Admin(), civ, "CIV101", "Statics", "الاستاتيكا");

        var byArabicName = await List("?search=قواعد");
        var byCode = await List("?search=cs30");
        var byDepartment = await List($"?departmentId={cs}");

        Assert.Equal(["CS302"], Codes(byArabicName));
        Assert.Equal(["CS301", "CS302"], Codes(byCode));                        // ordered by code
        Assert.Equal(["CS301", "CS302"], Codes(byDepartment));
        Assert.Equal(2, (int)byDepartment["totalCount"]!);
    }

    [Fact]
    public async Task Deactivated_subjects_are_hidden_unless_asked_for()
    {
        var (_, cs, _) = await Organization();
        var id = await CreateSubjectAsync(Admin(), cs, "CS301");

        var update = await Admin().PutAsJsonAsync($"/api/admin/subjects/{id}", new
        {
            name = "Algorithms (old)", nameAr = "الخوارزميات", description = (string?)null, creditHours = 3,
            category = "regular", isGraduationRequirement = false, isActive = false,
        });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("Algorithms (old)", (string?)(await BodyOf(update))["name"]);
        Assert.Empty(Codes(await List("")));
        Assert.Equal(["CS301"], Codes(await List("?includeInactive=true")));
    }

    [Fact]
    public async Task Military_education_is_stored_as_a_graduation_requirement_subject()
    {
        var (_, cs, _) = await Organization();

        var response = await PostSubjectAsync(Admin(), cs, "MIL100", "Military education", "التربية العسكرية",
            creditHours: 0, category: "military_education", isGraduationRequirement: true);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var subject = await BodyOf(response);
        Assert.Equal("military_education", (string?)subject["category"]);
        Assert.True((bool)subject["isGraduationRequirement"]!);
    }

    [Fact]
    public async Task Unknown_subject_is_a_404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await Student().GetAsync($"/api/subjects/{Guid.CreateVersion7()}")).StatusCode);
    }

    [Fact]
    public async Task Only_admins_manage_the_catalog()
    {
        var (_, cs, _) = await Organization();
        var body = new { departmentId = cs, code = "CS301", name = "A", nameAr = "أ", creditHours = 3, category = "regular", isGraduationRequirement = false };

        Assert.Equal(HttpStatusCode.Forbidden, (await Factory.CreateClientAs("professor").PostAsJsonAsync("/api/admin/subjects", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Factory.CreateClient().GetAsync("/api/subjects")).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private HttpClient Student() => Factory.CreateClientAs("student");

    private async Task<(Guid Eng, Guid Cs, Guid Civ)> Organization()
    {
        var eng = await OrganizationSetup.CreateFacultyAsync(Admin(), "ENG");
        return (eng, await OrganizationSetup.CreateDepartmentAsync(Admin(), eng, "CS"), await OrganizationSetup.CreateDepartmentAsync(Admin(), eng, "CIV"));
    }

    private async Task<JsonObject> List(string query) => (await Student().GetFromJsonAsync<JsonObject>("/api/subjects" + query))!;

    private static List<string?> Codes(JsonObject page) => page["items"]!.AsArray().Select(s => (string?)s!["code"]).ToList();
}
