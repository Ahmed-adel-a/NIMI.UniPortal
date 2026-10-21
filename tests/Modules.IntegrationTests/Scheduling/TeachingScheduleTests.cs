// ACCEPTANCE TEST — written by the consultant for phase P03. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Users;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Scheduling;

/// <summary>
/// P3.6 — a professor's own teaching occurrences (rules R9, R10). Hany teaches CS301 (all groups); Mona teaches CS302.
/// First week of term: Saturday 2026-09-19 … Friday 2026-09-25.
/// </summary>
public sealed class TeachingScheduleTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    private const int Sunday = 1;
    private const int Monday = 2;
    private const int Tuesday = 3;
    private const int Thursday = 5;
    private const string FirstWeek = "?from=2026-09-19&to=2026-09-25";

    [Fact]
    public async Task A_professor_sees_exactly_their_occurrences_in_the_range_ordered_by_date_and_time()
    {
        var w = await World();

        var hany = await ScheduleAsync(w.HanyClient, FirstWeek);
        var mona = await ScheduleAsync(w.MonaClient, FirstWeek);

        Assert.Equal(["2026-09-20 09:00:00 CS301/1", "2026-09-21 11:00:00 CS301/2"], Summaries(hany));
        Assert.Equal(["2026-09-20 12:00:00 CS302/1"], Summaries(mona));
        var first = hany[0]!;
        Assert.Equal(Sunday, (int)first["dayOfWeek"]!);
        Assert.Equal("10:30:00", (string?)first["endTime"]);
        Assert.Equal(w.Cs301Group1Slot, (Guid)first["scheduleId"]!);
        Assert.Equal("Algorithms", (string?)first["subjectName"]);
        Assert.Equal("الخوارزميات", (string?)first["subjectNameAr"]);
        Assert.Equal("Hall A", (string?)first["facilityName"]);
    }

    [Fact]
    public async Task A_longer_range_repeats_weekly_slots()
    {
        var w = await World();

        var twoWeeks = await ScheduleAsync(w.HanyClient, "?from=2026-09-19&to=2026-10-02");

        Assert.Equal(
            ["2026-09-20 09:00:00 CS301/1", "2026-09-21 11:00:00 CS301/2", "2026-09-27 09:00:00 CS301/1", "2026-09-28 11:00:00 CS301/2"],
            Summaries(twoWeeks));
    }

    [Fact]
    public async Task Being_named_on_a_slot_counts_even_without_an_assignment_for_that_group()
    {
        var w = await World();
        await CreateSlotAsync(Admin(), w.Cs302Group1, w.HallB, w.Hany, Tuesday, "14:00:00", "15:00:00");

        var hany = await ScheduleAsync(w.HanyClient, FirstWeek);

        Assert.Contains("2026-09-22 14:00:00 CS302/1", Summaries(hany));
    }

    [Fact]
    public async Task Removing_the_assignment_removes_the_groups_taught_through_it()
    {
        var w = await World();
        await Admin().DeleteAsync($"/api/admin/teaching-assignments/{w.HanyAssignment}");

        var hany = await ScheduleAsync(w.HanyClient, FirstWeek);

        Assert.Equal(["2026-09-20 09:00:00 CS301/1"], Summaries(hany));   // still named on that slot
    }

    [Fact]
    public async Task Cancelled_slots_are_hidden_and_a_once_slot_appears_only_on_its_date()
    {
        var w = await World();
        await Admin().PostAsync($"/api/admin/schedules/{w.Cs301Group1Slot}/cancel", null);
        await CreateSlotAsync(Admin(), w.Cs301Group1, w.HallA, w.Hany, Thursday, "12:00:00", "14:00:00", "once", "2026-09-24");

        var firstWeek = await ScheduleAsync(w.HanyClient, FirstWeek);
        var secondWeek = await ScheduleAsync(w.HanyClient, "?from=2026-09-26&to=2026-10-02");

        Assert.Equal(["2026-09-21 11:00:00 CS301/2", "2026-09-24 12:00:00 CS301/1"], Summaries(firstWeek));
        Assert.Equal(["2026-09-28 11:00:00 CS301/2"], Summaries(secondWeek));
    }

    [Fact]
    public async Task The_range_is_at_most_31_days_and_must_be_in_order()
    {
        var w = await World();

        var thirtyOneDays = await w.HanyClient.GetAsync("/api/teaching/schedule?from=2026-09-19&to=2026-10-19");
        var thirtyTwoDays = await w.HanyClient.GetAsync("/api/teaching/schedule?from=2026-09-19&to=2026-10-20");
        var backwards = await w.HanyClient.GetAsync("/api/teaching/schedule?from=2026-09-25&to=2026-09-19");
        var missing = await w.HanyClient.GetAsync("/api/teaching/schedule");

        Assert.Equal(HttpStatusCode.OK, thirtyOneDays.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, thirtyTwoDays.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, backwards.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
    }

    [Fact]
    public async Task Only_professors_have_a_teaching_schedule()
    {
        var w = await World();
        var (_, studentToken) = await AccountSetup.CreateStudentWithTokenAsync(Factory, Admin(), w.DepartmentId, "ahmed@cu.edu.eg");

        var student = await Factory.CreateClientWithToken(studentToken).GetAsync("/api/teaching/schedule" + FirstWeek);
        var admin = await Admin().GetAsync("/api/teaching/schedule" + FirstWeek);
        var anonymous = await Factory.CreateClient().GetAsync("/api/teaching/schedule" + FirstWeek);

        Assert.Equal(HttpStatusCode.Forbidden, student.StatusCode);
        Assert.Equal("forbidden", (string?)(await BodyOf(student))["code"]);
        Assert.Equal(HttpStatusCode.Forbidden, admin.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private static async Task<JsonArray> ScheduleAsync(HttpClient client, string query)
    {
        var response = await client.GetAsync("/api/teaching/schedule" + query);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonArray>())!;
    }

    private static List<string> Summaries(JsonArray occurrences) =>
        occurrences.Select(o => $"{(string?)o!["date"]} {(string?)o["startTime"]} {(string?)o["subjectCode"]}/{(int)o["groupNumber"]!}").ToList();

    private async Task<TeachingWorld> World()
    {
        var eng = await OrganizationSetup.CreateFacultyAsync(Admin(), "ENG");
        var cs = await OrganizationSetup.CreateDepartmentAsync(Admin(), eng, "CS");
        var termId = await CreateActiveTermAsync(Admin());
        var cs301 = await CreateSubjectAsync(Admin(), cs, "CS301", "Algorithms", "الخوارزميات");
        var cs302 = await CreateSubjectAsync(Admin(), cs, "CS302", "Databases", "قواعد البيانات");
        var cs301Group1 = await CreateGroupAsync(Admin(), cs301, termId, 1);
        var cs301Group2 = await CreateGroupAsync(Admin(), cs301, termId, 2);
        var cs302Group1 = await CreateGroupAsync(Admin(), cs302, termId, 1);
        var hallA = await CreateFacilityAsync(Admin(), "Hall A", eng);
        var hallB = await CreateFacilityAsync(Admin(), "Hall B", eng);
        var (hany, hanyToken) = await AccountSetup.CreateProfessorWithTokenAsync(Factory, Admin(), cs, "hany@cu.edu.eg");
        var (mona, monaToken) = await AccountSetup.CreateProfessorWithTokenAsync(Factory, Admin(), cs, "mona@cu.edu.eg");
        var hanyAssignment = await AssignTeacherAsync(Admin(), hany, cs301, termId);
        await AssignTeacherAsync(Admin(), mona, cs302, termId);

        var cs301Group1Slot = await CreateSlotAsync(Admin(), cs301Group1, hallA, hany, Sunday, "09:00:00", "10:30:00");
        await CreateSlotAsync(Admin(), cs301Group2, hallB, null, Monday, "11:00:00", "12:30:00");     // no staff named: via assignment
        await CreateSlotAsync(Admin(), cs302Group1, hallA, mona, Sunday, "12:00:00", "13:30:00");

        return new TeachingWorld(
            DepartmentId: cs,
            Cs301Group1: cs301Group1,
            Cs302Group1: cs302Group1,
            HallA: hallA,
            HallB: hallB,
            Hany: hany,
            HanyAssignment: hanyAssignment,
            Cs301Group1Slot: cs301Group1Slot,
            HanyClient: Factory.CreateClientWithToken(hanyToken),
            MonaClient: Factory.CreateClientWithToken(monaToken));
    }
}
