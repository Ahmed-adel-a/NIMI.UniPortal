// ACCEPTANCE TEST — written by the consultant for the grades CSV import (decision BACKENDDONE). Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Grades.GradesSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Grades;

/// <summary>
/// The teacher downloads the group's roster as a CSV template, fills in scores and uploads it back as drafts: all or
/// nothing, rows without a score skipped, every bad row reported with its line. Same rules as typed drafts.
/// </summary>
public sealed class GradeImportTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task The_template_lists_the_roster_with_student_codes()
    {
        var w = await GradedCampusAsync(Factory, Factory.CreateClientAs("admin"));
        var (ahmed, mona) = await CodesAsync(w);

        var response = await w.HanyClient.GetAsync($"/api/grades/groups/{w.Cs301Group1}/template");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        var csv = await response.Content.ReadAsStringAsync();
        Assert.StartsWith("student_code,full_name,score", csv.TrimStart('﻿'));
        Assert.Contains(ahmed, csv);
        Assert.Contains(mona, csv);
    }

    [Fact]
    public async Task A_filled_template_drafts_the_group()
    {
        var w = await GradedCampusAsync(Factory, Factory.CreateClientAs("admin"));
        var (ahmed, mona) = await CodesAsync(w);

        var response = await ImportAsync(w.HanyClient, w.Cs301Group1, $"student_code,full_name,score\n{ahmed},Ahmed,91.5\n{mona},Mona,58\n");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var grades = await GroupGradesAsync(w.HanyClient, w.Cs301Group1);
        Assert.Equal(91.5m, (decimal)Row(grades, w.Ahmed)["score"]!);
        Assert.Equal(58m, (decimal)Row(grades, w.Mona)["score"]!);
        Assert.Equal("draft", (string?)Row(grades, w.Ahmed)["status"]);
    }

    [Fact]
    public async Task Rows_without_a_score_are_skipped()
    {
        var w = await GradedCampusAsync(Factory, Factory.CreateClientAs("admin"));
        var (ahmed, mona) = await CodesAsync(w);

        var response = await ImportAsync(w.HanyClient, w.Cs301Group1, $"student_code;score\n{ahmed};77\n{mona};\n");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var grades = await GroupGradesAsync(w.HanyClient, w.Cs301Group1);
        Assert.Equal(77m, (decimal)Row(grades, w.Ahmed)["score"]!);
        Assert.Null(Row(grades, w.Mona)["score"]);
    }

    [Fact]
    public async Task Any_bad_row_saves_nothing_and_every_problem_names_its_line()
    {
        var w = await GradedCampusAsync(Factory, Factory.CreateClientAs("admin"));
        var (ahmed, mona) = await CodesAsync(w);

        var response = await ImportAsync(w.HanyClient, w.Cs301Group1,
            $"student_code,score\n{ahmed},88\nNOT-A-CODE,70\n{mona},101\n{ahmed},60\n");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var fields = await FieldsOf(response);
        Assert.Contains("rows[2].studentCode", fields);
        Assert.Contains("rows[3].score", fields);
        Assert.Contains("rows[4].studentCode", fields);
        Assert.Null(Row(await GroupGradesAsync(w.HanyClient, w.Cs301Group1), w.Ahmed)["score"]);
    }

    [Fact]
    public async Task A_file_without_the_columns_or_without_scores_is_rejected()
    {
        var w = await GradedCampusAsync(Factory, Factory.CreateClientAs("admin"));
        var (ahmed, _) = await CodesAsync(w);

        var noColumns = await ImportAsync(w.HanyClient, w.Cs301Group1, "code,mark\nX,1\n");
        var noScores = await ImportAsync(w.HanyClient, w.Cs301Group1, $"student_code,score\n{ahmed},\n");

        Assert.Equal(HttpStatusCode.BadRequest, noColumns.StatusCode);
        Assert.Contains("file", await FieldsOf(noColumns));
        Assert.Equal(HttpStatusCode.BadRequest, noScores.StatusCode);
        Assert.Contains("file", await FieldsOf(noScores));
    }

    [Fact]
    public async Task Only_the_teacher_imports_and_confirmed_grades_are_locked()
    {
        var admin = Factory.CreateClientAs("admin");
        var w = await GradedCampusAsync(Factory, admin);
        var (ahmed, mona) = await CodesAsync(w);
        var csv = $"student_code,score\n{ahmed},80\n{mona},90\n";

        Assert.Equal(HttpStatusCode.Forbidden, (await ImportAsync(w.AhmedClient, w.Cs301Group1, csv)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await ImportAsync(w.HanyClient, w.Cs301Group1, csv)).StatusCode);
        await ConfirmedAsync(admin, w.Cs301Group1);
        Assert.Equal(HttpStatusCode.Conflict, (await ImportAsync(w.HanyClient, w.Cs301Group1, csv)).StatusCode);
    }

    private static async Task<(string Ahmed, string Mona)> CodesAsync(Lifecycle.CampusWorld w)
    {
        var grades = await GroupGradesAsync(w.HanyClient, w.Cs301Group1);
        return ((string)Row(grades, w.Ahmed)["studentCode"]!, (string)Row(grades, w.Mona)["studentCode"]!);
    }

    private static async Task<HttpResponseMessage> ImportAsync(HttpClient client, Guid groupId, string csv)
    {
        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(file, "file", "grades.csv");
        return await client.PostAsync($"/api/grades/groups/{groupId}/import", content);
    }
}
