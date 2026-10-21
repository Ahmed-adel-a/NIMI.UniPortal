// ACCEPTANCE TEST — written by the consultant for phase P03. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Users;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Scheduling;

/// <summary>P3.5 — timetable slots and conflict detection (rules R6–R9).</summary>
public sealed class TimetableTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    private const int Saturday = 0;
    private const int Sunday = 1;
    private const int Thursday = 5;
    private const int Friday = 6;

    [Fact]
    public async Task A_slot_is_created_with_its_details_and_runs_until_the_end_of_the_term()
    {
        var w = await World();

        var response = await PostSlotAsync(Admin(), w.Cs301Group1, w.HallA, w.Professor, Sunday, "09:00:00", "10:30:00");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var slot = await BodyOf(response);
        Assert.Equal(w.Cs301Group1, (Guid)slot["subjectGroupId"]!);
        Assert.Equal("CS301", (string?)slot["subjectCode"]);
        Assert.Equal(1, (int)slot["groupNumber"]!);
        Assert.Equal(w.HallA, (Guid)slot["facilityId"]!);
        Assert.Equal("Hall A", (string?)slot["facilityName"]);
        Assert.Equal(w.Professor, (Guid)slot["staffId"]!);
        Assert.Equal(Sunday, (int)slot["dayOfWeek"]!);
        Assert.Equal("09:00:00", (string?)slot["startTime"]);
        Assert.Equal("10:30:00", (string?)slot["endTime"]);
        Assert.Equal("weekly", (string?)slot["recurrence"]);
        Assert.Equal(TermStart, (string?)slot["validFrom"]);
        Assert.Equal(TermEnd, (string?)slot["validUntil"]);
        Assert.False((bool)slot["isCancelled"]!);
        var listed = (await Admin().GetFromJsonAsync<JsonObject>($"/api/admin/schedules?termId={w.TermId}"))!;
        Assert.Equal(1, (int)listed["totalCount"]!);
    }

    [Fact]
    public async Task The_same_hall_at_an_overlapping_time_is_a_409_naming_the_facility_and_the_other_slot()
    {
        var w = await World();
        var existing = await CreateSlotAsync(Admin(), w.Cs301Group1, w.HallA, null, Sunday, "09:00:00", "10:30:00");

        var response = await PostSlotAsync(Admin(), w.Cs302Group1, w.HallA, null, Sunday, "10:00:00", "11:00:00");

        await AssertConflict(response, "facility", existing);
    }

    [Fact]
    public async Task The_same_group_in_another_hall_at_an_overlapping_time_is_a_409_naming_the_group()
    {
        var w = await World();
        var existing = await CreateSlotAsync(Admin(), w.Cs301Group1, w.HallA, null, Sunday, "09:00:00", "10:30:00");

        var response = await PostSlotAsync(Admin(), w.Cs301Group1, w.HallB, null, Sunday, "10:00:00", "11:00:00");

        await AssertConflict(response, "group", existing);
    }

    [Fact]
    public async Task The_same_staff_member_elsewhere_at_an_overlapping_time_is_a_409_naming_the_staff()
    {
        var w = await World();
        var existing = await CreateSlotAsync(Admin(), w.Cs301Group1, w.HallA, w.Professor, Sunday, "09:00:00", "10:30:00");

        var response = await PostSlotAsync(Admin(), w.Cs302Group1, w.HallB, w.Professor, Sunday, "10:00:00", "11:00:00");

        await AssertConflict(response, "staff", existing);
    }

    [Fact]
    public async Task Back_to_back_slots_in_the_same_hall_are_fine()
    {
        var w = await World();
        await CreateSlotAsync(Admin(), w.Cs301Group1, w.HallA, w.Professor, Sunday, "09:00:00", "10:30:00");

        var response = await PostSlotAsync(Admin(), w.Cs302Group1, w.HallA, w.Professor, Sunday, "10:30:00", "12:00:00");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task A_cancelled_slot_no_longer_blocks_the_hall()
    {
        var w = await World();
        var cancelled = await CreateSlotAsync(Admin(), w.Cs301Group1, w.HallA, null, Sunday, "09:00:00", "10:30:00");

        var cancel = await Admin().PostAsync($"/api/admin/schedules/{cancelled}/cancel", null);
        var replacement = await PostSlotAsync(Admin(), w.Cs302Group1, w.HallA, null, Sunday, "09:00:00", "10:30:00");

        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);
        Assert.Equal(HttpStatusCode.Created, replacement.StatusCode);
        var listed = (await Admin().GetFromJsonAsync<JsonObject>($"/api/admin/schedules?groupId={w.Cs301Group1}"))!;
        Assert.True((bool)listed["items"]![0]!["isCancelled"]!);
    }

    [Fact]
    public async Task Biweekly_slots_on_alternate_weeks_share_a_hall_but_not_on_the_same_weeks()
    {
        var w = await World();
        await CreateSlotAsync(Admin(), w.Cs301Group1, w.HallA, null, Sunday, "09:00:00", "10:30:00", "biweekly", "2026-09-20");

        var alternateWeeks = await PostSlotAsync(Admin(), w.Cs302Group1, w.HallA, null, Sunday, "09:00:00", "10:30:00", "biweekly", "2026-09-27");
        var sameWeeks = await PostSlotAsync(Admin(), w.Cs301Group2, w.HallA, null, Sunday, "09:00:00", "10:30:00", "biweekly", "2026-10-04");

        Assert.Equal(HttpStatusCode.Created, alternateWeeks.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, sameWeeks.StatusCode);
    }

    [Fact]
    public async Task A_slot_on_a_day_off_is_a_400_on_dayOfWeek()
    {
        var w = await World();

        var friday = await PostSlotAsync(Admin(), w.Cs301Group1, w.HallA, null, Friday, "09:00:00", "10:30:00");

        Assert.Equal(HttpStatusCode.BadRequest, friday.StatusCode);
        Assert.Contains("dayOfWeek", await FieldsOf(friday));
    }

    [Fact]
    public async Task Working_days_come_from_the_effective_settings_of_the_subjects_faculty()
    {
        var w = await World();
        var overrides = await Admin().PutAsJsonAsync($"/api/admin/faculties/{w.FacultyId}/settings", new { workingDays = new[] { 1, 2, 3, 4, 5 } });
        Assert.Equal(HttpStatusCode.OK, overrides.StatusCode);

        var saturday = await PostSlotAsync(Admin(), w.Cs301Group1, w.HallA, null, Saturday, "09:00:00", "10:30:00");
        var thursday = await PostSlotAsync(Admin(), w.Cs301Group1, w.HallA, null, Thursday, "09:00:00", "10:30:00");

        Assert.Equal(HttpStatusCode.BadRequest, saturday.StatusCode);   // a university working day, but not this faculty's
        Assert.Contains("dayOfWeek", await FieldsOf(saturday));
        Assert.Equal(HttpStatusCode.Created, thursday.StatusCode);
    }

    [Fact]
    public async Task A_slot_outside_the_term_is_a_400()
    {
        var w = await World();

        var afterTerm = await PostSlotAsync(Admin(), w.Cs301Group1, w.HallA, null, Sunday, "09:00:00", "10:30:00", validFrom: "2027-02-07");
        var endsBeforeStart = await PostSlotAsync(Admin(), w.Cs301Group1, w.HallA, null, Sunday, "10:30:00", "09:00:00");

        Assert.Equal(HttpStatusCode.BadRequest, afterTerm.StatusCode);
        Assert.Contains("validFrom", await FieldsOf(afterTerm));
        Assert.Equal(HttpStatusCode.BadRequest, endsBeforeStart.StatusCode);
        Assert.Contains("endTime", await FieldsOf(endsBeforeStart));
    }

    [Fact]
    public async Task Unknown_group_or_facility_is_a_400_on_that_field()
    {
        var w = await World();

        var unknownGroup = await PostSlotAsync(Admin(), Guid.CreateVersion7(), w.HallA, null, Sunday, "09:00:00", "10:30:00");
        var unknownHall = await PostSlotAsync(Admin(), w.Cs301Group1, Guid.CreateVersion7(), null, Sunday, "09:00:00", "10:30:00");

        Assert.Equal(HttpStatusCode.BadRequest, unknownGroup.StatusCode);
        Assert.Contains("subjectGroupId", await FieldsOf(unknownGroup));
        Assert.Equal(HttpStatusCode.BadRequest, unknownHall.StatusCode);
        Assert.Contains("facilityId", await FieldsOf(unknownHall));
    }

    [Fact]
    public async Task Only_an_active_professor_can_be_named_as_staff_on_a_slot()
    {
        var w = await World();
        var departmentId = (Guid)(await BodyOf(await Admin().GetAsync("/api/subjects?search=CS301")))["items"]![0]!["departmentId"]!;
        var (studentId, _) = await AccountSetup.CreateStudentWithTokenAsync(Factory, Admin(), departmentId, "ahmed@cu.edu.eg");

        var student = await PostSlotAsync(Admin(), w.Cs301Group1, w.HallA, studentId, Sunday, "09:00:00", "10:30:00");
        var unknown = await PostSlotAsync(Admin(), w.Cs301Group1, w.HallA, Guid.CreateVersion7(), Sunday, "09:00:00", "10:30:00");

        Assert.Equal(HttpStatusCode.BadRequest, student.StatusCode);
        Assert.Contains("staffId", await FieldsOf(student));
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Contains("staffId", await FieldsOf(unknown));
    }

    [Fact]
    public async Task Moving_a_slot_rechecks_conflicts_but_never_against_itself()
    {
        var w = await World();
        var slot = await CreateSlotAsync(Admin(), w.Cs301Group1, w.HallA, null, Sunday, "09:00:00", "10:30:00");
        var neighbour = await CreateSlotAsync(Admin(), w.Cs302Group1, w.HallA, null, Sunday, "11:00:00", "12:00:00");

        var shifted = await Admin().PutAsJsonAsync($"/api/admin/schedules/{slot}",
            SlotBody(w.Cs301Group1, w.HallA, null, Sunday, "09:30:00", "11:00:00"));
        var clash = await Admin().PutAsJsonAsync($"/api/admin/schedules/{slot}",
            SlotBody(w.Cs301Group1, w.HallA, null, Sunday, "10:30:00", "11:30:00"));

        Assert.Equal(HttpStatusCode.OK, shifted.StatusCode);
        Assert.Equal("09:30:00", (string?)(await BodyOf(shifted))["startTime"]);
        await AssertConflict(clash, "facility", neighbour);
    }

    [Fact]
    public async Task The_admin_timetable_is_filterable_and_ordered_by_day_then_time()
    {
        var w = await World();
        await CreateSlotAsync(Admin(), w.Cs301Group1, w.HallA, null, Thursday, "09:00:00", "10:00:00");
        await CreateSlotAsync(Admin(), w.Cs301Group2, w.HallA, null, Sunday, "11:00:00", "12:00:00");
        await CreateSlotAsync(Admin(), w.Cs302Group1, w.HallA, null, Sunday, "09:00:00", "10:00:00");
        await CreateSlotAsync(Admin(), w.Cs301Group1, w.HallB, null, Sunday, "13:00:00", "14:00:00");

        var hallA = (await Admin().GetFromJsonAsync<JsonObject>($"/api/admin/schedules?facilityId={w.HallA}"))!;

        Assert.Equal(3, (int)hallA["totalCount"]!);
        Assert.Equal(
            ["1 09:00:00", "1 11:00:00", "5 09:00:00"],
            hallA["items"]!.AsArray().Select(s => $"{(int)s!["dayOfWeek"]!} {(string?)s["startTime"]}"));
    }

    [Fact]
    public async Task Only_admins_manage_the_timetable()
    {
        var w = await World();
        var professor = Factory.CreateClientAs("professor");

        Assert.Equal(HttpStatusCode.Forbidden, (await professor.PostAsJsonAsync("/api/admin/schedules",
            SlotBody(w.Cs301Group1, w.HallA, null, Sunday, "09:00:00", "10:30:00"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await professor.GetAsync("/api/admin/schedules")).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private static async Task AssertConflict(HttpResponseMessage response, string kind, Guid otherSlotId)
    {
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = await BodyOf(response);
        Assert.Equal("conflict", (string?)error["code"]);
        var message = (string?)error["message"] ?? "";
        Assert.Contains(kind, message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(otherSlotId.ToString(), message, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<(Guid FacultyId, Guid TermId, Guid Cs301Group1, Guid Cs301Group2, Guid Cs302Group1, Guid HallA, Guid HallB, Guid Professor)> World()
    {
        var eng = await OrganizationSetup.CreateFacultyAsync(Admin(), "ENG");
        var cs = await OrganizationSetup.CreateDepartmentAsync(Admin(), eng, "CS");
        var termId = await CreateActiveTermAsync(Admin());
        var cs301 = await CreateSubjectAsync(Admin(), cs, "CS301");
        var cs302 = await CreateSubjectAsync(Admin(), cs, "CS302", "Databases", "قواعد البيانات");
        var (professor, _) = await AccountSetup.CreateProfessorWithTokenAsync(Factory, Admin(), cs, "hany@cu.edu.eg");
        return (
            eng,
            termId,
            await CreateGroupAsync(Admin(), cs301, termId, 1),
            await CreateGroupAsync(Admin(), cs301, termId, 2),
            await CreateGroupAsync(Admin(), cs302, termId, 1),
            await CreateFacilityAsync(Admin(), "Hall A", eng),
            await CreateFacilityAsync(Admin(), "Hall B", eng),
            professor);
    }
}
