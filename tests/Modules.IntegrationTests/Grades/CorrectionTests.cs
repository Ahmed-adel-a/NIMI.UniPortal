// ACCEPTANCE TEST — written by the consultant for phase P09. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Lifecycle;
using static NIMI.UniPortal.Modules.IntegrationTests.Grades.GradesSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Grades;

/// <summary>P9 corrections — only published grades, only Student Affairs, always with a reason and an audit row (R4).</summary>
public sealed class CorrectionTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task Student_affairs_corrects_a_published_grade_with_an_audit_row()
    {
        var w = await Campus();
        await GradeAndPublishAsync(w.HanyClient, Admin(), w.TermId, w.Cs301Group1, (w.Ahmed, 72m), (w.Mona, 80m));
        var gradeId = await GradeIdAsync(w, w.Ahmed);
        var before = await UnreadAsync(w.AhmedClient);

        var response = await CorrectAsync(Admin(), gradeId, 85m, "Exam re-marked after an addition error.");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var corrected = await BodyOf(response);
        Assert.Equal(85m, (decimal)corrected["score"]!);
        Assert.Equal("B+", (string?)corrected["grade"]);
        Assert.Equal("published", (string?)corrected["status"]);

        var audit = (await Admin().GetFromJsonAsync<JsonArray>($"/api/admin/grades/{gradeId}/corrections"))!;
        var row = Assert.Single(audit)!;
        Assert.Equal(72m, (decimal)row["oldScore"]!);
        Assert.Equal(85m, (decimal)row["newScore"]!);
        Assert.Equal("C", (string?)row["oldGrade"]);
        Assert.Equal("B+", (string?)row["newGrade"]);
        Assert.Equal("Exam re-marked after an addition error.", (string?)row["reason"]);

        var mine = (await MyGradesAsync(w.AhmedClient))["terms"]![0]!["grades"]![0]!;
        Assert.Equal("B+", (string?)mine["grade"]);
        Assert.Equal(before + 1, await UnreadAsync(w.AhmedClient));
    }

    [Fact]
    public async Task Only_published_grades_are_corrected()
    {
        var w = await Campus();
        await DraftedAsync(w.HanyClient, w.Cs301Group1, (w.Ahmed, 72m), (w.Mona, 80m));
        var gradeId = await GradeIdAsync(w, w.Ahmed);

        Assert.Equal(HttpStatusCode.Conflict, (await CorrectAsync(Admin(), gradeId, 85m, "Too early")).StatusCode);

        await ConfirmedAsync(Admin(), w.Cs301Group1);
        Assert.Equal(HttpStatusCode.Conflict, (await CorrectAsync(Admin(), gradeId, 85m, "Still too early")).StatusCode);
    }

    [Fact]
    public async Task A_correction_needs_a_reason_and_a_valid_score()
    {
        var w = await Campus();
        await GradeAndPublishAsync(w.HanyClient, Admin(), w.TermId, w.Cs301Group1, (w.Ahmed, 72m), (w.Mona, 80m));
        var gradeId = await GradeIdAsync(w, w.Ahmed);

        var noReason = await CorrectAsync(Admin(), gradeId, 85m, "  ");
        Assert.Equal(HttpStatusCode.BadRequest, noReason.StatusCode);
        Assert.Contains("reason", await FieldsOf(noReason));

        var badScore = await CorrectAsync(Admin(), gradeId, 120m, "Typo");
        Assert.Equal(HttpStatusCode.BadRequest, badScore.StatusCode);
        Assert.Contains("score", await FieldsOf(badScore));
    }

    [Fact]
    public async Task Only_student_affairs_corrects_and_unknown_grades_are_404()
    {
        var w = await Campus();
        await GradeAndPublishAsync(w.HanyClient, Admin(), w.TermId, w.Cs301Group1, (w.Ahmed, 72m), (w.Mona, 80m));
        var gradeId = await GradeIdAsync(w, w.Ahmed);

        Assert.Equal(HttpStatusCode.Forbidden, (await CorrectAsync(w.HanyClient, gradeId, 85m, "Mine")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await CorrectAsync(Admin(), Guid.CreateVersion7(), 85m, "Ghost")).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private Task<CampusWorld> Campus() => GradedCampusAsync(Factory, Admin());

    private async Task<Guid> GradeIdAsync(CampusWorld w, Guid studentId) =>
        (Guid)Row(await GroupGradesAsync(Admin(), w.Cs301Group1), studentId)["gradeId"]!;
}
