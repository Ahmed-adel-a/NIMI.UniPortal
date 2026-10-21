// ACCEPTANCE TEST — written by the consultant for phase P09. Do not modify; report problems instead.
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.Grades.Contracts;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Lifecycle.RegistrationSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Lifecycle;

/// <summary>
/// P9.3b — the lifecycle answers <see cref="GradesPublished"/>: ledger rows, completed/failed registrations, GPA
/// snapshots and real progress. Events are published directly so this file does not depend on the grading workflow.
/// </summary>
public sealed class GradeLedgerTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task Published_grades_complete_or_fail_registrations_and_earn_credits()
    {
        var w = await Campus();
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);
        await RegisteredAsync(w.AhmedClient, w.Cs303Group1);

        await PublishGradesAsync(w.TermId, (w.Ahmed, w.Cs301, 3, 3.0m), (w.Ahmed, w.Cs303, 3, 0.0m));

        var registrations = (await MineAsync(w.AhmedClient))["registrations"]!.AsArray();
        Assert.Equal("completed", Status(registrations, w.Cs301));
        Assert.Equal("failed", Status(registrations, w.Cs303));
        var progress = await ProgressAsync(w.AhmedClient);
        Assert.Equal(3, (int)progress["creditsEarned"]!);
        Assert.Equal(0, (int)progress["creditsInProgress"]!);
        Assert.Equal(1.5m, (decimal)progress["cumulativeGpa"]!);
    }

    [Fact]
    public async Task Completed_and_failed_students_stay_on_the_group_roster()
    {
        var w = await Campus();
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);
        await RegisteredAsync(w.MonaClient, w.Cs301Group1);

        await PublishGradesAsync(w.TermId, (w.Ahmed, w.Cs301, 3, 3.0m), (w.Mona, w.Cs301, 3, 0.0m));

        var roster = (await Factory.CreateClientAs("admin").GetFromJsonAsync<JsonObject>($"/api/admin/groups/{w.Cs301Group1}/roster"))!;
        Assert.Equal(2, (int)roster["totalCount"]!);
    }

    [Fact]
    public async Task Cumulative_gpa_and_credits_count_the_latest_attempt_of_a_retaken_subject()
    {
        var w = await Campus();
        var second = await CreateTermAsync(
            Factory.CreateClientAs("admin"), academicYear: 2026, number: "second", startsOn: "2027-02-06", endsOn: "2027-06-04", weekCount: 17);

        await PublishGradesAsync(w.TermId, (w.Ahmed, w.Cs301, 3, 3.0m), (w.Ahmed, w.Cs303, 3, 0.0m));
        await PublishGradesAsync(second, (w.Ahmed, w.Cs303, 3, 4.0m));

        var progress = await ProgressAsync(w.AhmedClient);
        Assert.Equal(6, (int)progress["creditsEarned"]!);
        Assert.Equal(3.5m, (decimal)progress["cumulativeGpa"]!);
    }

    [Fact]
    public async Task A_republished_grade_replaces_the_ledger_row()
    {
        var w = await Campus();
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);

        await PublishGradesAsync(w.TermId, (w.Ahmed, w.Cs301, 3, 0.0m));
        await PublishGradesAsync(w.TermId, (w.Ahmed, w.Cs301, 3, 2.0m));      // a correction re-publishes

        var progress = await ProgressAsync(w.AhmedClient);
        Assert.Equal(3, (int)progress["creditsEarned"]!);
        Assert.Equal(2.0m, (decimal)progress["cumulativeGpa"]!);
        Assert.Equal("completed", Status((await MineAsync(w.AhmedClient))["registrations"]!.AsArray(), w.Cs301));
    }

    [Fact]
    public async Task A_passed_graduation_requirement_is_no_longer_outstanding()
    {
        var w = await Campus();

        await PublishGradesAsync(w.TermId, (w.Ahmed, w.Mil100, 0, 1.0m));

        var progress = await ProgressAsync(w.AhmedClient);
        Assert.Empty(progress["outstandingRequirements"]!.AsArray());
        Assert.Null(progress["cumulativeGpa"]);                // zero credits carry no GPA
    }

    [Fact]
    public async Task A_passed_prerequisite_unlocks_registration()
    {
        var w = await Campus();
        var admin = Factory.CreateClientAs("admin");
        Assert.Equal(System.Net.HttpStatusCode.OK, (await SetPrerequisitesAsync(admin, w.Cs302, w.Cs301)).StatusCode);
        var second = await CreateTermAsync(admin, academicYear: 2025, number: "second", startsOn: "2026-02-07", endsOn: "2026-06-05", weekCount: 17);

        await PublishGradesAsync(second, (w.Ahmed, w.Cs301, 3, 3.0m));

        Assert.Equal(System.Net.HttpStatusCode.Created, (await RegisterAsync(w.AhmedClient, w.Cs302Group1)).StatusCode);
        Assert.NotEqual(System.Net.HttpStatusCode.Created, (await RegisterAsync(w.MonaClient, w.Cs302Group1)).StatusCode);
    }

    private Task<CampusWorld> Campus() => BuildCampusAsync(Factory, Factory.CreateClientAs("admin"));

    private Task PublishGradesAsync(Guid termId, params (Guid Student, Guid Subject, int Credits, decimal Points)[] grades) =>
        Factory.PublishAsync(new GradesPublished(
            Guid.CreateVersion7(),
            Factory.Clock.GetUtcNow(),
            termId,
            [.. grades.Select(g => new PublishedGrade(g.Student, g.Subject, g.Credits, g.Points, g.Points > 0))]));

    private static async Task<JsonObject> ProgressAsync(HttpClient student) =>
        (await student.GetFromJsonAsync<JsonObject>("/api/registration/progress"))!;

    private static string? Status(JsonArray registrations, Guid subjectId) =>
        (string?)registrations.Single(r => (Guid)r!["subjectId"]! == subjectId)!["status"];
}
