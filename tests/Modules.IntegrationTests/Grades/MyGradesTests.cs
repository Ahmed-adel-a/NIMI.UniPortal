// ACCEPTANCE TEST — written by the consultant for phase P09. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Lifecycle;
using static NIMI.UniPortal.Modules.IntegrationTests.Grades.GradesSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Lifecycle.RegistrationSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Grades;

/// <summary>P9.3 — the student's published grades with term and cumulative GPA, on the faculty's own scale (R1, R5).</summary>
public sealed class MyGradesTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task Grades_and_gpa_use_the_subject_facultys_scale_weighted_by_credits()
    {
        var w = await BuildCampusAsync(Factory, Admin());
        var scale = await Admin().PutAsJsonAsync($"/api/admin/faculties/{w.FacultyId}/settings", new
        {
            gradingScale = new[]
            {
                new { grade = "A", minScore = 90m, points = 4.0m },
                new { grade = "B", minScore = 80m, points = 3.0m },
                new { grade = "C", minScore = 70m, points = 2.0m },
                new { grade = "F", minScore = 0m, points = 0.0m },
            },
        });
        Assert.Equal(HttpStatusCode.OK, scale.StatusCode);
        var cs304 = await CreateSubjectAsync(Admin(), w.CsDepartmentId, "CS304", "Compilers", "المترجمات", creditHours: 2);
        var cs304Group1 = await CreateGroupAsync(Admin(), cs304, w.TermId, 1);
        await AssignTeacherAsync(Admin(), w.Hany, cs304, w.TermId);
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);
        await RegisteredAsync(w.AhmedClient, cs304Group1);

        await GradeAndPublishAsync(w.HanyClient, Admin(), w.TermId, w.Cs301Group1, (w.Ahmed, 85m));   // B 3.0 × 3
        await GradeAndPublishAsync(w.HanyClient, Admin(), w.TermId, cs304Group1, (w.Ahmed, 95m));     // A 4.0 × 2

        var mine = await MyGradesAsync(w.AhmedClient);
        var term = mine["terms"]!.AsArray().Single()!;
        Assert.Equal(2026, (int)term["academicYear"]!);
        Assert.Equal("first", (string?)term["number"]);
        Assert.Equal(3.4m, (decimal)term["termGpa"]!);
        Assert.Equal(3.4m, (decimal)mine["cumulativeGpa"]!);
        var grades = term["grades"]!.AsArray();
        Assert.Equal(["CS301", "CS304"], grades.Select(g => (string?)g!["subjectCode"]));
        var cs301 = grades[0]!;
        Assert.Equal("B", (string?)cs301["grade"]);
        Assert.Equal(3.0m, (decimal)cs301["gradePoints"]!);
        Assert.Equal(3, (int)cs301["creditHours"]!);
        Assert.Equal(85m, (decimal)cs301["score"]!);
        Assert.Equal("Algorithms", (string?)cs301["subjectName"]);
        Assert.Equal("الخوارزميات", (string?)cs301["subjectNameAr"]);
        Assert.NotNull(cs301["gradeId"]);
    }

    [Fact]
    public async Task A_term_filter_limits_the_terms()
    {
        var w = await GradedCampusAsync(Factory, Admin());
        await GradeAndPublishAsync(w.HanyClient, Admin(), w.TermId, w.Cs301Group1, (w.Ahmed, 85m), (w.Mona, 60m));
        var otherTerm = await CreateTermAsync(Admin(), academicYear: 2026, number: "second", startsOn: "2027-02-06", endsOn: "2027-06-04", weekCount: 17);

        Assert.Single((await MyGradesAsync(w.AhmedClient, w.TermId))["terms"]!.AsArray());
        Assert.Empty((await MyGradesAsync(w.AhmedClient, otherTerm))["terms"]!.AsArray());
    }

    [Fact]
    public async Task Students_see_only_their_own_grades()
    {
        var w = await GradedCampusAsync(Factory, Admin());
        await GradeAndPublishAsync(w.HanyClient, Admin(), w.TermId, w.Cs301Group1, (w.Ahmed, 85m), (w.Mona, 60m));

        var mona = (await MyGradesAsync(w.MonaClient))["terms"]![0]!["grades"]!.AsArray().Single()!;

        Assert.Equal(60m, (decimal)mona["score"]!);
        Assert.Equal("D", (string?)mona["grade"]);
    }

    [Fact]
    public async Task Only_students_have_grades()
    {
        var w = await BuildCampusAsync(Factory, Admin());

        Assert.Equal(HttpStatusCode.Forbidden, (await w.HanyClient.GetAsync("/api/grades/mine")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Admin().GetAsync("/api/grades/mine")).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");
}
