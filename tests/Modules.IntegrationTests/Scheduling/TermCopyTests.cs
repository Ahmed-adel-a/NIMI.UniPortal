using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Users;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Scheduling;

/// <summary>
/// Creating a term from an earlier one: <c>POST /api/admin/terms/{id}/copy</c> makes a planned term and carries over the
/// groups, teaching assignments and weekly/biweekly timetable slots. Students' data and one-off slots stay behind, and
/// whatever cannot be carried over is reported instead of failing the copy.
/// </summary>
public sealed class TermCopyTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    private const string NewStart = "2027-09-18";    // Saturday, one year after TermStart
    private const string NewEnd = "2028-01-14";      // Friday, 17 weeks later
    private const int Saturday = 0;
    private const int Sunday = 1;
    private const int Monday = 2;
    private const string UnreadCount = "/api/notifications/mine/unread-count";

    [Fact]
    public async Task The_copy_is_a_planned_term_and_the_source_is_left_alone()
    {
        var w = await WorldAsync();

        var response = await CopyAsync(w.Source);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await BodyOf(response);
        var term = result["term"]!;
        Assert.Equal(2027, (int)term["academicYear"]!);
        Assert.Equal("first", (string?)term["number"]);
        Assert.Equal(NewStart, (string?)term["startsOn"]);
        Assert.Equal(NewEnd, (string?)term["endsOn"]);
        Assert.Equal(17, (int)term["weekCount"]!);
        Assert.Equal("planned", (string?)term["status"]);
        var source = (await Admin().GetFromJsonAsync<JsonObject>("/api/terms?pageSize=100"))!["items"]!.AsArray()
            .Single(t => (Guid)t!["id"]! == w.Source)!;
        Assert.Equal("active", (string?)source["status"]);
    }

    [Fact]
    public async Task Groups_are_copied_with_their_number_capacity_and_target_department()
    {
        var w = await WorldAsync();

        var result = await BodyOf(await CopyAsync(w.Source));

        var copied = await GroupsOfAsync(w.Cs301, NewTermId(result));
        Assert.Equal(new[] { 1, 2 }, copied.Select(g => (int)g!["groupNumber"]!).Order().ToArray());
        Assert.Equal(80, (int)copied.Single(g => (int)g!["groupNumber"]! == 1)!["capacity"]!);
        Assert.Equal(w.Cs, (Guid)copied.Single(g => (int)g!["groupNumber"]! == 2)!["targetDepartmentId"]!);
        Assert.All(copied, g => Assert.Equal(0, (int)g!["registeredCount"]!));
        Assert.Equal(3, (int)result["copied"]!["groups"]!);        // CS301 ×2 + CS302 ×1
    }

    [Fact]
    public async Task Teaching_assignments_are_copied_for_the_whole_subject_and_for_single_groups()
    {
        var w = await WorldAsync();

        var result = await BodyOf(await CopyAsync(w.Source));

        var assignments = (await Admin().GetFromJsonAsync<JsonObject>(
            $"/api/admin/teaching-assignments?termId={NewTermId(result)}&pageSize=100"))!["items"]!.AsArray();
        Assert.Equal(2, assignments.Count);
        var wholeSubject = assignments.Single(a => a!["subjectGroupId"] is null)!;
        Assert.Equal(w.Hany, (Guid)wholeSubject["staffId"]!);
        Assert.Equal("CS301", (string?)wholeSubject["subjectCode"]);
        var singleGroup = assignments.Single(a => a!["subjectGroupId"] is not null)!;
        Assert.Equal("lab", (string?)singleGroup["role"]);
        Assert.Equal(2, (int)singleGroup["groupNumber"]!);
        Assert.Equal(2, (int)result["copied"]!["teachingAssignments"]!);
    }

    [Fact]
    public async Task Weekly_and_biweekly_slots_are_copied_keeping_their_week_in_the_term()
    {
        var w = await WorldAsync();

        var result = await BodyOf(await CopyAsync(w.Source));

        var slots = await SlotsOfAsync(NewTermId(result));
        Assert.Equal(3, slots.Count);
        var weekly = slots.Single(s => (string?)s!["recurrence"] == "weekly" && (int)s["dayOfWeek"]! == Sunday)!;
        Assert.Equal("09:00:00", (string?)weekly["startTime"]);
        Assert.Equal("10:30:00", (string?)weekly["endTime"]);
        Assert.Equal(w.HallA, (Guid)weekly["facilityId"]!);
        Assert.Equal(w.Hany, (Guid)weekly["staffId"]!);
        Assert.Equal(NewStart, (string?)weekly["validFrom"]);
        Assert.Equal(NewEnd, (string?)weekly["validUntil"]);
        Assert.Contains(slots, s => (string?)s!["recurrence"] == "biweekly");
        var partial = slots.Single(s => (int)s!["dayOfWeek"]! == Saturday)!;           // ran from week 3 to week 11 in the source
        Assert.Equal("2027-10-02", (string?)partial["validFrom"]);
        Assert.Equal("2027-11-26", (string?)partial["validUntil"]);
        Assert.Equal(3, (int)result["copied"]!["slots"]!);
    }

    [Fact]
    public async Task One_off_and_cancelled_slots_stay_behind()
    {
        var w = await WorldAsync();
        await CreateSlotAsync(Admin(), w.Cs302Group1, w.HallB, null, Saturday, "13:00:00", "14:00:00", "once", "2026-10-10", "2026-10-10");
        var cancelled = await CreateSlotAsync(Admin(), w.Cs302Group1, w.HallB, null, Monday, "13:00:00", "14:00:00");
        Assert.Equal(HttpStatusCode.NoContent, (await Admin().PostAsync($"/api/admin/schedules/{cancelled}/cancel", null)).StatusCode);

        var result = await BodyOf(await CopyAsync(w.Source));

        Assert.Equal(3, (await SlotsOfAsync(NewTermId(result))).Count);
        Assert.Empty(result["skipped"]!.AsArray());                  // left behind on purpose, not "skipped"
    }

    [Fact]
    public async Task What_cannot_be_carried_over_is_skipped_and_reported_while_the_rest_is_copied()
    {
        var w = await WorldAsync();
        var inactiveSubject = await CreateSubjectAsync(Admin(), w.Cs, "CS401", "Compilers", "المترجمات");
        await CreateGroupAsync(Admin(), inactiveSubject, w.Source);
        Assert.Equal(HttpStatusCode.OK, (await Admin().PutAsJsonAsync($"/api/admin/subjects/{inactiveSubject}", new
        {
            name = "Compilers", nameAr = "المترجمات", description = (string?)null, creditHours = 3, category = "regular",
            isGraduationRequirement = false, isActive = false,
        })).StatusCode);
        var hallC = await CreateFacilityAsync(Admin(), "Hall C", w.Faculty);
        await CreateSlotAsync(Admin(), w.Cs302Group1, hallC, null, Monday, "11:00:00", "12:00:00");
        Assert.Equal(HttpStatusCode.OK, (await Admin().PutAsJsonAsync($"/api/admin/facilities/{hallC}", new
        {
            facultyId = w.Faculty, name = "Hall C", type = "lecture_hall", capacity = 120, building = "Main building", floor = "1", isActive = false,
        })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Admin().PatchAsJsonAsync($"/api/admin/users/{w.Mona}/status", new { isActive = false })).StatusCode);

        var result = await BodyOf(await CopyAsync(w.Source));

        var skipped = result["skipped"]!.AsArray();
        Assert.Contains(skipped, s => (string?)s!["kind"] == "group" && (string?)s["subjectCode"] == "CS401" && (string?)s["reason"] == "subject_inactive");
        Assert.Contains(skipped, s => (string?)s!["kind"] == "slot" && (string?)s["subjectCode"] == "CS302" && (string?)s["reason"] == "facility_inactive");
        Assert.Contains(skipped, s => (string?)s!["kind"] == "teaching_assignment" && (string?)s["reason"] == "staff_inactive");
        Assert.Equal(3, (int)result["copied"]!["groups"]!);          // the inactive subject's group is the only one left out
        Assert.Empty(await GroupsOfAsync(inactiveSubject, NewTermId(result)));
    }

    [Fact]
    public async Task A_slot_that_clashes_with_an_existing_one_is_skipped_and_the_others_still_copy()
    {
        var w = await WorldAsync();

        // A term starting two weeks into the source: each copied slot lands on dates its own original already uses,
        // in the same hall, group and (for the first) lecturer, and the biweekly slot falls on the same weeks.
        var response = await CopyAsync(w.Source, academicYear: 2026, number: "second", startsOn: "2026-10-03", endsOn: "2027-01-29");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await BodyOf(response);
        Assert.Equal(0, (int)result["copied"]!["slots"]!);
        Assert.Equal(3, result["skipped"]!.AsArray().Count(s => (string?)s!["kind"] == "slot" && (string?)s["reason"] == "conflict"));
        Assert.Equal(3, (int)result["copied"]!["groups"]!);
        Assert.Equal(2, (int)result["copied"]!["teachingAssignments"]!);
    }

    [Fact]
    public async Task The_same_year_and_term_number_twice_is_a_409()
    {
        var w = await WorldAsync();
        Assert.Equal(HttpStatusCode.Created, (await CopyAsync(w.Source)).StatusCode);

        var again = await CopyAsync(w.Source);

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("conflict", (string?)(await BodyOf(again))["code"]);
    }

    [Fact]
    public async Task An_unknown_source_is_a_404_and_bad_dates_are_a_400_naming_the_field()
    {
        var w = await WorldAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await CopyAsync(Guid.CreateVersion7())).StatusCode);
        var bad = await CopyAsync(w.Source, startsOn: NewEnd, endsOn: NewStart);
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        Assert.Contains("endsOn", await FieldsOf(bad));
    }

    [Fact]
    public async Task Only_student_affairs_may_copy_a_term()
    {
        var w = await WorldAsync();

        foreach (var role in new[] { "professor", "student" })
            Assert.Equal(HttpStatusCode.Forbidden, (await CopyAsync(w.Source, Factory.CreateClientAs(role))).StatusCode);
    }

    [Fact]
    public async Task A_draft_notifies_nobody_and_the_copy_is_in_the_activity_log()
    {
        var w = await WorldAsync();
        var hany = Factory.CreateClientWithToken(w.HanyToken);
        var before = (int)(await hany.GetFromJsonAsync<JsonObject>(UnreadCount))!["count"]!;

        var result = await BodyOf(await CopyAsync(w.Source));

        Assert.Equal(before, (int)(await hany.GetFromJsonAsync<JsonObject>(UnreadCount))!["count"]!);
        var rows = (await Admin().GetFromJsonAsync<JsonObject>($"/api/admin/activity?entityType=term&entityId={NewTermId(result)}"))!["items"]!.AsArray();
        var row = Assert.Single(rows)!;
        Assert.Equal("term.copied", (string?)row["action"]);
    }

    [Fact]
    public async Task The_copy_can_be_activated_and_becomes_the_current_term_with_its_timetable()
    {
        var w = await WorldAsync();
        var newTerm = NewTermId(await BodyOf(await CopyAsync(w.Source)));

        Assert.Equal(HttpStatusCode.OK, (await Admin().PostAsync($"/api/admin/terms/{newTerm}/activate", null)).StatusCode);

        var current = (await Admin().GetFromJsonAsync<JsonObject>("/api/terms/current"))!;
        Assert.Equal(newTerm, (Guid)current["id"]!);
        Assert.Equal(3, (await SlotsOfAsync(newTerm)).Count);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private Task<HttpResponseMessage> CopyAsync(
        Guid source, HttpClient? client = null, int academicYear = 2027, string number = "first",
        string startsOn = NewStart, string endsOn = NewEnd, int weekCount = 17) =>
        (client ?? Admin()).PostAsJsonAsync($"/api/admin/terms/{source}/copy", new { academicYear, number, startsOn, endsOn, weekCount });

    private static Guid NewTermId(JsonObject result) => (Guid)result["term"]!["id"]!;

    private async Task<JsonArray> GroupsOfAsync(Guid subjectId, Guid termId) =>
        (await Admin().GetFromJsonAsync<JsonObject>($"/api/subjects/{subjectId}/groups?termId={termId}&pageSize=100"))!["items"]!.AsArray();

    private async Task<JsonArray> SlotsOfAsync(Guid termId) =>
        (await Admin().GetFromJsonAsync<JsonObject>($"/api/admin/schedules?termId={termId}&pageSize=100"))!["items"]!.AsArray();

    /// <summary>
    /// The 2026 first term (active) with CS301 (groups 1 and 2) and CS302 (group 1), two professors, whole-subject and
    /// single-group teaching assignments, and three slots: a weekly one, a biweekly one and a weekly one that only ran
    /// from week 3 to week 11.
    /// </summary>
    private async Task<CopyWorld> WorldAsync()
    {
        var faculty = await OrganizationSetup.CreateFacultyAsync(Admin(), "ENG");
        var cs = await OrganizationSetup.CreateDepartmentAsync(Admin(), faculty, "CS");
        var source = await CreateActiveTermAsync(Admin());
        var cs301 = await CreateSubjectAsync(Admin(), cs, "CS301");
        var cs302 = await CreateSubjectAsync(Admin(), cs, "CS302", "Databases", "قواعد البيانات");
        var (hany, hanyToken) = await AccountSetup.CreateProfessorWithTokenAsync(Factory, Admin(), cs, "hany@cu.edu.eg");
        var (mona, _) = await AccountSetup.CreateProfessorWithTokenAsync(Factory, Admin(), cs, "mona@cu.edu.eg");

        var group1 = await CreateGroupAsync(Admin(), cs301, source, 1, 80);
        var group2 = (await BodyOf(await PostGroupAsync(Admin(), cs301, source, 2, 60, targetDepartmentId: cs)))["id"]!.GetValue<Guid>();
        var cs302Group1 = await CreateGroupAsync(Admin(), cs302, source, 1);
        var hallA = await CreateFacilityAsync(Admin(), "Hall A", faculty);
        var hallB = await CreateFacilityAsync(Admin(), "Hall B", faculty);

        await AssignTeacherAsync(Admin(), hany, cs301, source);
        await AssignTeacherAsync(Admin(), mona, cs301, source, group2, "lab");

        await CreateSlotAsync(Admin(), group1, hallA, hany, Sunday, "09:00:00", "10:30:00");
        await CreateSlotAsync(Admin(), group2, hallA, null, Sunday, "11:00:00", "12:30:00", "biweekly");
        await CreateSlotAsync(Admin(), cs302Group1, hallB, null, Saturday, "09:00:00", "10:30:00", "weekly", "2026-10-03", "2026-11-27");

        return new CopyWorld(faculty, cs, source, cs301, cs302Group1, hallA, hallB, hany, hanyToken, mona);
    }
}
