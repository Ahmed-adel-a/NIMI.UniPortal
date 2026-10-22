// ACCEPTANCE TEST — written by the consultant for phase P4.B. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Users;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.SemesterBased.SemesterSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.SemesterBased;

/// <summary>
/// P4.7–P4.8: generating a term places new students in level 1 and enrolls them in their level's plan subjects for
/// that term, spread over each subject's groups; rosters, the student's standing and the cohorts follow from it.
/// </summary>
public sealed class TermEnrollmentTests(ReferenceHostFactory factory) : ReferenceHostTest(factory)
{
    [Fact]
    public async Task Generating_places_new_students_in_level_one_with_the_plan_subjects_of_the_term()
    {
        var admin = Factory.CreateClientAs("admin");
        var w = await BuildAsync(Factory, admin);

        var report = await GeneratedAsync(admin, w.TermId, w.Department);

        Assert.Equal(2, (int)report["newStandings"]!);
        Assert.Equal(8, (int)report["enrolled"]!);   // S1–S4 for Ahmed and Mona; F1 is a first-term subject
        Assert.Empty(report["unplaced"]!.AsArray());
        var ahmed = await EnrollmentsAsync(admin, w.TermId, w.Ahmed);
        Assert.Equal(
            new[] { w.Subjects["S1"], w.Subjects["S2"], w.Subjects["S3"], w.Subjects["S4"] }.Order(),
            ahmed.Select(e => (Guid)e!["subjectId"]!).Order());
        Assert.All(ahmed, e => Assert.False((bool)e!["isCarried"]!));

        var standing = await StandingAsync(w.AhmedClient);
        Assert.Equal(1, (int)standing["level"]!);
        Assert.Equal(4, (int)standing["finalLevel"]!);
        Assert.Equal("active", (string?)standing["status"]);
    }

    [Fact]
    public async Task Students_are_spread_over_the_groups_of_a_subject()
    {
        var admin = Factory.CreateClientAs("admin");
        var w = await BuildAsync(Factory, admin);

        await GeneratedAsync(admin, w.TermId);

        var group1 = (await admin.GetFromJsonAsync<JsonObject>($"/api/admin/groups/{w.Groups["S1"]}/roster"))!;
        var group2 = (await admin.GetFromJsonAsync<JsonObject>($"/api/admin/groups/{w.Groups["S1#2"]}/roster"))!;
        Assert.Equal(1, (int)group1["totalCount"]!);
        Assert.Equal(1, (int)group2["totalCount"]!);
    }

    [Fact]
    public async Task Generating_again_changes_nothing()
    {
        var admin = Factory.CreateClientAs("admin");
        var w = await BuildAsync(Factory, admin);
        await GeneratedAsync(admin, w.TermId);

        var again = await GeneratedAsync(admin, w.TermId);

        Assert.Equal(0, (int)again["enrolled"]!);
        Assert.Equal(8, (int)again["alreadyEnrolled"]!);
        Assert.Equal(0, (int)again["newStandings"]!);
    }

    [Fact]
    public async Task A_department_without_a_study_plan_is_reported_not_enrolled()
    {
        var admin = Factory.CreateClientAs("admin");
        var w = await BuildAsync(Factory, admin);
        var math = await OrganizationSetup.CreateDepartmentAsync(admin, w.Faculty, "MATH");
        var (sara, _) = await AccountSetup.CreateStudentWithTokenAsync(Factory, admin, math, "sara@asu.edu.eg");

        var report = await GeneratedAsync(admin, w.TermId);

        var unplaced = Assert.Single(report["unplaced"]!.AsArray());
        Assert.Equal(sara, (Guid)unplaced!["studentId"]!);
        Assert.Contains("study plan", (string?)unplaced["reason"]);
    }

    [Fact]
    public async Task The_teacher_sees_the_roster_and_student_affairs_can_move_a_student()
    {
        var admin = Factory.CreateClientAs("admin");
        var w = await BuildAsync(Factory, admin);
        await GeneratedAsync(admin, w.TermId);

        var roster = (await w.HanyClient.GetFromJsonAsync<JsonObject>($"/api/teaching/groups/{w.Groups["S2"]}/roster"))!;
        Assert.Equal(2, (int)roster["totalCount"]!);

        var s1 = (await EnrollmentsAsync(admin, w.TermId, w.Ahmed)).Single(e => (Guid)e!["subjectId"]! == w.Subjects["S1"])!;
        var other = (Guid)s1["subjectGroupId"]! == w.Groups["S1"] ? w.Groups["S1#2"] : w.Groups["S1"];
        var moved = await admin.PutAsJsonAsync($"/api/admin/term-enrollments/{s1["id"]}/group", new { subjectGroupId = other });
        Assert.Equal(HttpStatusCode.OK, moved.StatusCode);
        Assert.Equal(other, (Guid)(await BodyOf(moved))["subjectGroupId"]!);

        var wrong = await admin.PutAsJsonAsync($"/api/admin/term-enrollments/{s1["id"]}/group", new { subjectGroupId = w.Groups["S2"] });
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
    }

    [Fact]
    public async Task Cohorts_count_each_level_and_student_affairs_can_place_a_student()
    {
        var admin = Factory.CreateClientAs("admin");
        var w = await BuildAsync(Factory, admin);
        await GeneratedAsync(admin, w.TermId);

        var placed = await admin.PutAsJsonAsync($"/api/admin/standings/{w.Mona}", new { level = 3 });
        Assert.Equal(HttpStatusCode.OK, placed.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.PutAsJsonAsync($"/api/admin/standings/{w.Mona}", new { level = 5 })).StatusCode);

        var cohorts = (await admin.GetFromJsonAsync<JsonArray>($"/api/admin/cohorts?departmentId={w.Department}"))!;
        Assert.Equal([1, 0, 1, 0], cohorts.Select(c => (int)c!["total"]!).ToArray());
    }
}
