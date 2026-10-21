// ACCEPTANCE TEST — written by the consultant for phase P09. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.Grades.Contracts;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Users;
using static NIMI.UniPortal.Modules.IntegrationTests.Lifecycle.RegistrationSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Lifecycle;

/// <summary>
/// P9.4 — who can graduate (R6, MILEDU, GRADREQ): earned credits ≥ the department's requirement and every
/// graduation-requirement subject (military education) passed. Grades arrive as <see cref="GradesPublished"/> events.
/// </summary>
public sealed class GraduationEligibilityTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task A_department_without_a_requirement_blocks_its_students()
    {
        var w = await Campus();
        await PublishGradesAsync(w.TermId, (w.Ahmed, w.Cs301, 3, 3.0m), (w.Ahmed, w.Mil100, 0, 1.0m));

        var ahmed = Student(await EligibilityAsync($"departmentId={w.CsDepartmentId}"), w.Ahmed);

        Assert.False((bool)ahmed["eligible"]!);
        Assert.Null(ahmed["creditsRequired"]);
        Assert.Equal(["requirement_not_configured"], Blockers(ahmed));
    }

    [Fact]
    public async Task Too_few_credits_block_graduation()
    {
        var w = await Campus();
        await RequireAsync(w.CsDepartmentId, 6);
        await PublishGradesAsync(w.TermId, (w.Ahmed, w.Cs301, 3, 3.0m), (w.Ahmed, w.Mil100, 0, 1.0m));

        var ahmed = Student(await EligibilityAsync($"departmentId={w.CsDepartmentId}"), w.Ahmed);

        Assert.False((bool)ahmed["eligible"]!);
        Assert.Equal(3, (int)ahmed["creditsEarned"]!);
        Assert.Equal(6, (int)ahmed["creditsRequired"]!);
        Assert.Equal(["credits"], Blockers(ahmed));
    }

    [Fact]
    public async Task Military_education_not_passed_blocks_graduation()
    {
        var w = await Campus();
        await RequireAsync(w.CsDepartmentId, 3);
        await PublishGradesAsync(w.TermId, (w.Ahmed, w.Cs301, 3, 3.0m), (w.Mona, w.Cs301, 3, 3.0m), (w.Mona, w.Mil100, 0, 0.0m));

        var list = await EligibilityAsync($"departmentId={w.CsDepartmentId}");

        Assert.Equal(["required_subject:MIL100"], Blockers(Student(list, w.Ahmed)));     // never taken
        Assert.Equal(["required_subject:MIL100"], Blockers(Student(list, w.Mona)));      // taken and failed
    }

    [Fact]
    public async Task Enough_credits_and_military_education_make_a_student_eligible()
    {
        var w = await Campus();
        await RequireAsync(w.CsDepartmentId, 3);
        await PublishGradesAsync(w.TermId, (w.Ahmed, w.Cs301, 3, 3.0m), (w.Ahmed, w.Mil100, 0, 1.0m));

        var ahmed = Student(await EligibilityAsync($"departmentId={w.CsDepartmentId}"), w.Ahmed);

        Assert.True((bool)ahmed["eligible"]!);
        Assert.Empty(Blockers(ahmed));
        Assert.Equal(3, (int)ahmed["creditsEarned"]!);
        Assert.NotNull(ahmed["studentCode"]);
        Assert.NotNull(ahmed["fullName"]);
        Assert.Equal(w.CsDepartmentId, (Guid)ahmed["departmentId"]!);
    }

    [Fact]
    public async Task Each_department_applies_its_own_requirement()
    {
        var w = await Campus();
        var (omar, _) = await AccountSetup.CreateStudentWithTokenAsync(Factory, Admin(), w.CivDepartmentId, "omar@cu.edu.eg");
        await RequireAsync(w.CsDepartmentId, 3);
        await RequireAsync(w.CivDepartmentId, 6);
        await PublishGradesAsync(
            w.TermId,
            (w.Ahmed, w.Cs301, 3, 3.0m), (w.Ahmed, w.Mil100, 0, 1.0m),
            (omar, w.Civ101, 3, 3.0m), (omar, w.Mil100, 0, 1.0m));

        var faculty = await EligibilityAsync($"facultyId={w.FacultyId}");
        Assert.Equal(3, (int)faculty["totalCount"]!);                               // Ahmed, Mona, Omar
        Assert.True((bool)Student(faculty, w.Ahmed)["eligible"]!);
        Assert.Equal(["credits"], Blockers(Student(faculty, omar)));
        Assert.Equal(6, (int)Student(faculty, omar)["creditsRequired"]!);

        var civil = await EligibilityAsync($"departmentId={w.CivDepartmentId}");
        Assert.Equal([omar], civil["items"]!.AsArray().Select(i => (Guid)i!["studentId"]!));
    }

    [Fact]
    public async Task Several_blockers_are_listed_together_and_the_list_is_paged()
    {
        var w = await Campus();
        await RequireAsync(w.CsDepartmentId, 6);
        await PublishGradesAsync(w.TermId, (w.Ahmed, w.Cs301, 3, 3.0m));

        var all = await EligibilityAsync($"departmentId={w.CsDepartmentId}");
        Assert.Equal(["credits", "required_subject:MIL100"], Blockers(Student(all, w.Ahmed)));

        var page = await EligibilityAsync($"departmentId={w.CsDepartmentId}&page=2&pageSize=1");
        Assert.Equal(2, (int)page["totalCount"]!);
        Assert.Single(page["items"]!.AsArray());
    }

    [Fact]
    public async Task Only_student_affairs_sees_eligibility()
    {
        var w = await Campus();

        Assert.Equal(HttpStatusCode.Forbidden, (await w.HanyClient.GetAsync("/api/admin/graduation/eligibility")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await w.AhmedClient.GetAsync("/api/admin/graduation/eligibility")).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private Task<CampusWorld> Campus() => BuildCampusAsync(Factory, Admin());

    private async Task RequireAsync(Guid departmentId, int requiredCredits)
    {
        var response = await Admin().PutAsJsonAsync($"/api/admin/graduation/requirements/{departmentId}", new { requiredCredits });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<JsonObject> EligibilityAsync(string query) =>
        (await Admin().GetFromJsonAsync<JsonObject>($"/api/admin/graduation/eligibility?{query}"))!;

    private static JsonNode Student(JsonObject list, Guid studentId) =>
        list["items"]!.AsArray().Single(i => (Guid)i!["studentId"]! == studentId)!;

    private static List<string?> Blockers(JsonNode student) =>
        student["blockers"]!.AsArray().Select(b => (string?)b).ToList();

    private Task PublishGradesAsync(Guid termId, params (Guid Student, Guid Subject, int Credits, decimal Points)[] grades) =>
        Factory.PublishAsync(new GradesPublished(
            Guid.CreateVersion7(),
            Factory.Clock.GetUtcNow(),
            termId,
            [.. grades.Select(g => new PublishedGrade(g.Student, g.Subject, g.Credits, g.Points, g.Points > 0))]));
}
