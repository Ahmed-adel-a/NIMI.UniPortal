// ACCEPTANCE TEST — written by the consultant for phase P09. Do not modify; report problems instead.
using System.Net;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Lifecycle;
using static NIMI.UniPortal.Modules.IntegrationTests.Grades.GradesSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Grades;

/// <summary>P9.1 — the group's teaching staff draft scores for roster students (D3, rules R2 and R5).</summary>
public sealed class GradeDraftingTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task The_teacher_drafts_scores_and_sees_letters_from_the_scale()
    {
        var w = await Campus();

        var body = await DraftedAsync(w.HanyClient, w.Cs301Group1, (w.Ahmed, 95m), (w.Mona, 72m));

        Assert.Equal(w.Cs301Group1, (Guid)body["groupId"]!);
        Assert.Equal(w.Cs301, (Guid)body["subjectId"]!);
        Assert.Equal("CS301", (string?)body["subjectCode"]);
        Assert.Equal(3, (int)body["creditHours"]!);
        var ahmed = Row(body, w.Ahmed);
        Assert.Equal(95m, (decimal)ahmed["score"]!);
        Assert.Equal("A", (string?)ahmed["grade"]);
        Assert.Equal(4.0m, (decimal)ahmed["gradePoints"]!);
        Assert.Equal("draft", (string?)ahmed["status"]);
        Assert.NotNull(ahmed["gradeId"]);
        Assert.NotNull(ahmed["studentCode"]);
        var mona = Row(body, w.Mona);
        Assert.Equal("C", (string?)mona["grade"]);
        Assert.Equal(2.0m, (decimal)mona["gradePoints"]!);
    }

    [Fact]
    public async Task The_roster_lists_students_without_a_grade_yet()
    {
        var w = await Campus();
        await DraftedAsync(w.HanyClient, w.Cs301Group1, (w.Ahmed, 80m));

        var body = await GroupGradesAsync(w.HanyClient, w.Cs301Group1);

        Assert.Equal(2, body["rows"]!.AsArray().Count);
        var mona = Row(body, w.Mona);
        Assert.Null(mona["gradeId"]);
        Assert.Null(mona["score"]);
        Assert.Null(mona["status"]);
        Assert.Equal("B", (string?)Row(body, w.Ahmed)["grade"]);
    }

    [Fact]
    public async Task Drafting_again_overwrites_the_draft()
    {
        var w = await Campus();
        var first = await DraftedAsync(w.HanyClient, w.Cs301Group1, (w.Ahmed, 95m));

        var second = await DraftedAsync(w.HanyClient, w.Cs301Group1, (w.Ahmed, 85m));

        var row = Row(second, w.Ahmed);
        Assert.Equal((Guid)Row(first, w.Ahmed)["gradeId"]!, (Guid)row["gradeId"]!);
        Assert.Equal(85m, (decimal)row["score"]!);
        Assert.Equal("B+", (string?)row["grade"]);
        Assert.Equal(3.3m, (decimal)row["gradePoints"]!);
    }

    [Fact]
    public async Task Only_the_groups_teaching_staff_draft()
    {
        var w = await Campus();
        var (_, otherToken) = await AccountSetup.CreateProfessorWithTokenAsync(Factory, Admin(), w.CsDepartmentId, "other@cu.edu.eg");
        var other = Factory.CreateClientWithToken(otherToken);

        Assert.Equal(HttpStatusCode.Forbidden, (await PutDraftsAsync(other, w.Cs301Group1, (w.Ahmed, 90m))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await PutDraftsAsync(Admin(), w.Cs301Group1, (w.Ahmed, 90m))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await PutDraftsAsync(w.AhmedClient, w.Cs301Group1, (w.Ahmed, 90m))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"/api/grades/groups/{w.Cs301Group1}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await w.AhmedClient.GetAsync($"/api/grades/groups/{w.Cs301Group1}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Admin().GetAsync($"/api/grades/groups/{w.Cs301Group1}")).StatusCode);
    }

    [Fact]
    public async Task A_teacher_assigned_to_one_group_drafts_only_that_group()
    {
        var w = await Campus();
        var (sami, samiToken) = await AccountSetup.CreateProfessorWithTokenAsync(Factory, Admin(), w.CsDepartmentId, "sami@cu.edu.eg");
        await AssignTeacherAsync(Admin(), sami, w.Cs301, w.TermId, w.Cs301Group2);
        var samiClient = Factory.CreateClientWithToken(samiToken);

        Assert.Equal(HttpStatusCode.Forbidden, (await PutDraftsAsync(samiClient, w.Cs301Group1, (w.Ahmed, 90m))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await samiClient.GetAsync($"/api/grades/groups/{w.Cs301Group2}")).StatusCode);
    }

    [Fact]
    public async Task Every_entry_must_be_a_roster_student_once()
    {
        var w = await Campus();
        var (stranger, _) = await AccountSetup.CreateStudentWithTokenAsync(Factory, Admin(), w.CsDepartmentId, "stranger@cu.edu.eg");

        var notOnRoster = await PutDraftsAsync(w.HanyClient, w.Cs301Group1, (w.Ahmed, 90m), (stranger, 70m));
        Assert.Equal(HttpStatusCode.BadRequest, notOnRoster.StatusCode);
        Assert.Contains("entries[1].studentId", await FieldsOf(notOnRoster));

        var twice = await PutDraftsAsync(w.HanyClient, w.Cs301Group1, (w.Ahmed, 90m), (w.Ahmed, 70m));
        Assert.Equal(HttpStatusCode.BadRequest, twice.StatusCode);
        Assert.Contains("entries[1].studentId", await FieldsOf(twice));

        Assert.Null(Row(await GroupGradesAsync(w.HanyClient, w.Cs301Group1), w.Ahmed)["score"]);   // nothing saved
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100.01)]
    [InlineData(70.555)]
    public async Task Scores_are_between_0_and_100_with_two_decimals(decimal score)
    {
        var w = await Campus();

        var response = await PutDraftsAsync(w.HanyClient, w.Cs301Group1, (w.Ahmed, score));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("entries[0].score", await FieldsOf(response));
    }

    [Fact]
    public async Task An_empty_draft_is_rejected()
    {
        var w = await Campus();

        var response = await PutDraftsAsync(w.HanyClient, w.Cs301Group1);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("entries", await FieldsOf(response));
    }

    [Fact]
    public async Task Confirmed_grades_are_no_longer_drafts()
    {
        var w = await Campus();
        await DraftedAsync(w.HanyClient, w.Cs301Group1, (w.Ahmed, 90m), (w.Mona, 80m));
        await ConfirmedAsync(Admin(), w.Cs301Group1);

        var response = await PutDraftsAsync(w.HanyClient, w.Cs301Group1, (w.Ahmed, 99m));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("confirmed", (string?)Row(await GroupGradesAsync(w.HanyClient, w.Cs301Group1), w.Ahmed)["status"]);
    }

    [Fact]
    public async Task An_unknown_group_is_404()
    {
        var w = await Campus();

        Assert.Equal(HttpStatusCode.NotFound, (await PutDraftsAsync(w.HanyClient, Guid.CreateVersion7(), (w.Ahmed, 90m))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Admin().GetAsync($"/api/grades/groups/{Guid.CreateVersion7()}")).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private Task<CampusWorld> Campus() => GradedCampusAsync(Factory, Admin());
}
