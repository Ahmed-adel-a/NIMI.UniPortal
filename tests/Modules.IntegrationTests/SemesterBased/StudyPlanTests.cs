// ACCEPTANCE TEST — written by the consultant for phase P4.B. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.SemesterBased.SemesterSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.SemesterBased;

/// <summary>P4.7: Student Affairs keeps each department's study plan: its levels and the subjects of each level and term.</summary>
public sealed class StudyPlanTests(ReferenceHostFactory factory) : ReferenceHostTest(factory)
{
    [Fact]
    public async Task A_saved_plan_comes_back_ordered_with_the_subject_codes()
    {
        var admin = Factory.CreateClientAs("admin");
        var w = await BuildAsync(Factory, admin);

        var plan = (await admin.GetFromJsonAsync<JsonObject>($"/api/admin/study-plans/{w.Department}"))!;

        Assert.Equal(4, (int)plan["levels"]!);
        var subjects = plan["subjects"]!.AsArray();
        Assert.Equal(
            ["F1", "S1", "S2", "S3", "S4", "T1"],
            subjects.Select(s => (string)s!["code"]!).ToArray());
        Assert.Equal("first", (string?)subjects[0]!["term"]);
        Assert.Equal(2, (int)subjects[5]!["level"]!);
    }

    [Fact]
    public async Task Saving_again_replaces_the_whole_plan()
    {
        var admin = Factory.CreateClientAs("admin");
        var w = await BuildAsync(Factory, admin);

        var response = await SavePlanAsync(admin, w.Department, 2, [new { level = 2, term = "first", subjectId = w.Subjects["T1"] }]);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var plan = await BodyOf(response);
        Assert.Equal(2, (int)plan["levels"]!);
        Assert.Single(plan["subjects"]!.AsArray());
    }

    [Fact]
    public async Task A_level_beyond_the_programme_or_an_unknown_subject_is_rejected()
    {
        var admin = Factory.CreateClientAs("admin");
        var w = await BuildAsync(Factory, admin, withPlan: false);

        var tooHigh = await SavePlanAsync(admin, w.Department, 2, [new { level = 3, term = "first", subjectId = w.Subjects["S1"] }]);
        var unknown = await SavePlanAsync(admin, w.Department, 2, [new { level = 1, term = "first", subjectId = Guid.CreateVersion7() }]);
        var twice = await SavePlanAsync(admin, w.Department, 2,
        [
            new { level = 1, term = "first", subjectId = w.Subjects["S1"] },
            new { level = 2, term = "second", subjectId = w.Subjects["S1"] },
        ]);

        Assert.Equal(HttpStatusCode.BadRequest, tooHigh.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, twice.StatusCode);
        Assert.Contains("subjects", await FieldsOf(unknown));
    }

    [Fact]
    public async Task A_department_without_a_plan_is_not_found_and_only_student_affairs_manage_plans()
    {
        var admin = Factory.CreateClientAs("admin");
        var w = await BuildAsync(Factory, admin, withPlan: false);

        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/admin/study-plans/{w.Department}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await w.AhmedClient.GetAsync($"/api/admin/study-plans/{w.Department}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SavePlanAsync(w.HanyClient, w.Department, 1, [])).StatusCode);
    }
}
