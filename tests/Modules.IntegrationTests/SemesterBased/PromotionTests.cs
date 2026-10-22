// ACCEPTANCE TEST — written by the consultant for phase P4.B. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Payments.PaymentsSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.SemesterBased.SemesterSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.SemesterBased;

/// <summary>
/// P4.9 / decision SEMPROMO: when Student Affairs publishes the second term's grades, each student whose results are
/// complete is decided automatically: promoted, promoted carrying failed subjects, repeating, or held until the fees
/// are paid. Student Affairs can override; failed subjects come back the next year.
/// </summary>
public sealed class PromotionTests(ReferenceHostFactory factory) : ReferenceHostTest(factory)
{
    [Fact]
    public async Task Passing_everything_promotes_the_student_when_grades_are_published()
    {
        var (admin, w) = await EnrolledAsync();

        await GradeYearAsync(w, admin, new Dictionary<Guid, IReadOnlyDictionary<string, decimal>>
        {
            [w.Ahmed] = AllPassed,
            [w.Mona] = AllPassed,
        });

        var standing = await StandingAsync(w.AhmedClient);
        Assert.Equal(2, (int)standing["level"]!);
        Assert.Equal("active", (string?)standing["status"]);
        Assert.Equal("promoted", (string?)standing["lastDecision"]!["result"]);
        Assert.Empty(standing["carriedSubjects"]!.AsArray());
        Assert.Contains("Promoted", await NotificationTitlesAsync(w.AhmedClient));
    }

    [Fact]
    public async Task Up_to_three_failed_subjects_are_carried_and_four_repeat_the_year()
    {
        var (admin, w) = await EnrolledAsync();

        await GradeYearAsync(w, admin, new Dictionary<Guid, IReadOnlyDictionary<string, decimal>>
        {
            [w.Ahmed] = Scores(30, 30, 30, 85),   // 3 failed
            [w.Mona] = Scores(30, 30, 30, 30),    // 4 failed
        });

        var ahmed = await StandingAsync(w.AhmedClient);
        Assert.Equal(2, (int)ahmed["level"]!);
        Assert.Equal("promoted_with_carried", (string?)ahmed["lastDecision"]!["result"]);
        Assert.Equal(3, ahmed["carriedSubjects"]!.AsArray().Count);

        var mona = await StandingAsync(w.MonaClient);
        Assert.Equal(1, (int)mona["level"]!);
        Assert.Equal("repeating", (string?)mona["status"]);
        Assert.Equal(2, (int)mona["yearsInLevel"]!);
    }

    [Fact]
    public async Task A_stage_gate_is_entered_only_with_everything_passed()
    {
        var (admin, w) = await EnrolledAsync();
        Assert.Equal(HttpStatusCode.OK, (await SavePolicyAsync(admin, gateLevels: [2], facultyId: w.Faculty)).StatusCode);

        await GradeYearAsync(w, admin, new Dictionary<Guid, IReadOnlyDictionary<string, decimal>>
        {
            [w.Ahmed] = Scores(30, 85, 85, 85),
            [w.Mona] = AllPassed,
        });

        Assert.Equal("repeat", (string?)(await StandingAsync(w.AhmedClient))["lastDecision"]!["result"]);
        Assert.Equal(2, (int)(await StandingAsync(w.MonaClient))["level"]!);
    }

    [Fact]
    public async Task Outstanding_fees_hold_the_decision_until_they_are_paid()
    {
        var (admin, w) = await EnrolledAsync();
        var tuition = await CreateFeeItemAsync(admin, "Tuition", "tuition", 1000m, w.TermId, departmentId: w.Department);
        await RecordedAsync(admin, w.Mona, tuition, 1000m);

        await GradeYearAsync(w, admin, new Dictionary<Guid, IReadOnlyDictionary<string, decimal>>
        {
            [w.Ahmed] = AllPassed,
            [w.Mona] = AllPassed,
        });

        var held = await StandingAsync(w.AhmedClient);
        Assert.Equal("on_hold", (string?)held["status"]);
        Assert.Equal(1, (int)held["level"]!);
        Assert.Equal(2, (int)(await StandingAsync(w.MonaClient))["level"]!);

        await RecordedAsync(admin, w.Ahmed, tuition, 1000m);

        var released = await StandingAsync(w.AhmedClient);
        Assert.Equal("active", (string?)released["status"]);
        Assert.Equal(2, (int)released["level"]!);
        var history = (await admin.GetFromJsonAsync<JsonObject>($"/api/admin/promotions?studentId={w.Ahmed}"))!["items"]!.AsArray();
        Assert.Equal(["promoted", "held"], history.Select(p => (string)p!["result"]!).ToArray());
    }

    [Fact]
    public async Task Student_affairs_can_override_the_rule_with_a_reason()
    {
        var (admin, w) = await EnrolledAsync();
        await GradeYearAsync(w, admin, new Dictionary<Guid, IReadOnlyDictionary<string, decimal>>
        {
            [w.Ahmed] = Scores(30, 30, 30, 30),
            [w.Mona] = AllPassed,
        });

        var noReason = await admin.PostAsJsonAsync("/api/admin/promotions/override", new { studentId = w.Ahmed, result = "promoted", reason = "" });
        Assert.Equal(HttpStatusCode.BadRequest, noReason.StatusCode);

        var overridden = await admin.PostAsJsonAsync("/api/admin/promotions/override",
            new { studentId = w.Ahmed, result = "promoted", reason = "Excused absence accepted by the faculty council." });
        Assert.Equal(HttpStatusCode.OK, overridden.StatusCode);
        var decision = await BodyOf(overridden);
        Assert.Equal(TestAuthHandler.UserIdFor("admin"), (Guid)decision["decidedBy"]!);
        Assert.Equal(2, (int)decision["toLevel"]!);
        Assert.Equal(2, (int)(await StandingAsync(w.AhmedClient))["level"]!);
    }

    [Fact]
    public async Task Next_year_carried_subjects_come_back_and_a_repeating_student_takes_only_the_failed_ones()
    {
        var (admin, w) = await EnrolledAsync();
        Assert.Equal(HttpStatusCode.OK, (await SavePolicyAsync(admin, maxCarriedSubjects: 1)).StatusCode);
        await GradeYearAsync(w, admin, new Dictionary<Guid, IReadOnlyDictionary<string, decimal>>
        {
            [w.Ahmed] = Scores(30, 85, 85, 85),   // 1 failed → carried to level 2
            [w.Mona] = Scores(30, 30, 85, 85),    // 2 failed → repeats level 1
        });

        var nextYear = await SecondTermAsync(admin, 2027, "2028-02-05", "2028-06-09");
        var groups = await GroupsAsync(admin, nextYear, w.Subjects, w.Hany);
        Assert.NotEmpty(groups);
        await GeneratedAsync(admin, nextYear);

        var ahmed = await EnrollmentsAsync(admin, nextYear, w.Ahmed);
        Assert.Equal(
            new[] { w.Subjects["S1"], w.Subjects["T1"] }.Order(),
            ahmed.Select(e => (Guid)e!["subjectId"]!).Order());
        Assert.True((bool)ahmed.Single(e => (Guid)e!["subjectId"]! == w.Subjects["S1"])!["isCarried"]!);

        var mona = await EnrollmentsAsync(admin, nextYear, w.Mona);
        Assert.Equal(
            new[] { w.Subjects["S1"], w.Subjects["S2"] }.Order(),
            mona.Select(e => (Guid)e!["subjectId"]!).Order());
    }

    [Fact]
    public async Task The_explicit_run_decides_nobody_twice()
    {
        var (admin, w) = await EnrolledAsync();
        await GradeYearAsync(w, admin, new Dictionary<Guid, IReadOnlyDictionary<string, decimal>>
        {
            [w.Ahmed] = AllPassed,
            [w.Mona] = AllPassed,
        });

        var run = await admin.PostAsJsonAsync("/api/admin/promotions/run", new { termId = w.TermId });

        Assert.Equal(HttpStatusCode.OK, run.StatusCode);
        var body = await BodyOf(run);
        Assert.Equal(0, (int)body["decided"]!);
        Assert.Equal(0, (int)body["waitingForResults"]!);
        Assert.Equal(2, (int)(await StandingAsync(w.AhmedClient))["level"]!);
    }

    private async Task<(HttpClient Admin, SemesterWorld World)> EnrolledAsync()
    {
        var admin = Factory.CreateClientAs("admin");
        var w = await BuildAsync(Factory, admin);
        await GeneratedAsync(admin, w.TermId);
        return (admin, w);
    }
}
