// ACCEPTANCE TEST — written by the consultant for phase P10. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Lifecycle;
using NIMI.UniPortal.Modules.IntegrationTests.Users;
using static NIMI.UniPortal.Modules.IntegrationTests.Grades.GradesSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Payments.PaymentsSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Audit;

/// <summary>
/// P10.2 — sensitive actions raise <c>AuditableActionPerformed</c> after their commit; the Audit module stores one row per
/// event and Student Affairs reads them newest first, filtered and paged.
/// </summary>
public sealed class ActivityLogTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    private static readonly Guid AdminId = TestAuthHandler.UserIdFor("admin");

    [Fact]
    public async Task Settings_changes_are_logged_with_the_actor()
    {
        var eng = await OrganizationSetup.CreateFacultyAsync(Admin(), "ENG");
        var settings = (await Admin().GetFromJsonAsync<JsonObject>("/api/admin/settings"))!;
        settings["lateGraceMinutes"] = 20;
        Assert.Equal(HttpStatusCode.OK, (await Admin().PutAsJsonAsync("/api/admin/settings", settings)).StatusCode);
        var overrides = $"/api/admin/faculties/{eng}/settings";
        Assert.Equal(HttpStatusCode.OK, (await Admin().PutAsJsonAsync(overrides, new { attendanceThreshold = 80 })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Admin().DeleteAsync(overrides)).StatusCode);

        var system = Assert.Single(Items(await ActivityAsync("entityType=system_settings")))!;
        Assert.Equal("settings.updated", (string?)system["action"]);
        Assert.Equal(AdminId, (Guid)system["actorId"]!);
        Assert.False(string.IsNullOrWhiteSpace((string?)system["summary"]));
        Assert.NotNull(system["occurredAt"]);

        var faculty = Items(await ActivityAsync($"entityType=faculty&entityId={eng}"));
        Assert.Equal(["faculty_settings.removed", "faculty_settings.updated"], Actions(faculty));
    }

    [Fact]
    public async Task Account_actions_are_logged_against_the_account()
    {
        var eng = await OrganizationSetup.CreateFacultyAsync(Admin(), "ENG");
        var cs = await OrganizationSetup.CreateDepartmentAsync(Admin(), eng, "CS");
        var (ahmed, _) = await AccountSetup.CreateStudentWithTokenAsync(Factory, Admin(), cs, "ahmed@cu.edu.eg");
        Assert.Equal(HttpStatusCode.NoContent, (await Admin().PatchAsJsonAsync($"/api/admin/users/{ahmed}/status", new { isActive = false })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Admin().PostAsync($"/api/admin/users/{ahmed}/reset-password", null)).StatusCode);

        var rows = Items(await ActivityAsync($"entityType=user&entityId={ahmed}"));

        Assert.Equal(["user.password_reset", "user.status_changed", "user.created"], Actions(rows));
        Assert.All(rows, r => Assert.Equal(AdminId, (Guid)r!["actorId"]!));
        Assert.DoesNotContain("Portal!", rows.Select(r => (string?)r!["summary"]).Aggregate(string.Concat));   // never a password
    }

    [Fact]
    public async Task Grading_and_payments_are_logged()
    {
        var w = await GradedCampusAsync(Factory, Admin());
        await GradeAndPublishAsync(w.HanyClient, Admin(), w.TermId, w.Cs301Group1, (w.Ahmed, 72m), (w.Mona, 80m));
        var gradeId = (Guid)Row(await GroupGradesAsync(Admin(), w.Cs301Group1), w.Ahmed)["gradeId"]!;
        Assert.Equal(HttpStatusCode.OK, (await CorrectAsync(Admin(), gradeId, 85m, "Re-marked")).StatusCode);
        var fee = await CreateFeeItemAsync(Admin(), "Tuition", "tuition", 500m, w.TermId);
        var payment = await RecordedAsync(Admin(), w.Ahmed, fee, 200m);

        Assert.Equal(["grades.confirmed"], Actions(Items(await ActivityAsync($"entityType=subject_group&entityId={w.Cs301Group1}"))));
        Assert.Equal(["grades.published"], Actions(Items(await ActivityAsync($"entityType=term&entityId={w.TermId}"))));
        Assert.Equal(["grade.corrected"], Actions(Items(await ActivityAsync($"entityType=final_grade&entityId={gradeId}"))));
        var paid = Assert.Single(Items(await ActivityAsync("entityType=payment")))!;
        Assert.Equal("payment.recorded", (string?)paid["action"]);
        Assert.Equal((Guid)payment["id"]!, (Guid)paid["entityId"]!);
    }

    [Fact]
    public async Task Timetable_edits_and_cancellations_are_logged()
    {
        var w = await BuildCampusAsync();
        var hall = await CreateFacilityAsync(Admin(), "Hall C", w.FacultyId);
        var slot = await CreateSlotAsync(Admin(), w.Cs301Group1, hall, null, 3, "11:00:00", "12:00:00");
        Assert.Equal(HttpStatusCode.OK, (await Admin().PutAsJsonAsync($"/api/admin/schedules/{slot}",
            SlotBody(w.Cs301Group1, hall, null, 3, "12:00:00", "13:00:00"))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Admin().PostAsync($"/api/admin/schedules/{slot}/cancel", null)).StatusCode);

        var rows = Items(await ActivityAsync($"entityType=schedule_slot&entityId={slot}"));

        Assert.Equal(["schedule_slot.cancelled", "schedule_slot.updated"], Actions(rows));
    }

    [Fact]
    public async Task The_log_filters_by_actor_and_time_and_is_paged()
    {
        var eng = await OrganizationSetup.CreateFacultyAsync(Admin(), "ENG");
        var cs = await OrganizationSetup.CreateDepartmentAsync(Admin(), eng, "CS");
        await AccountSetup.CreateStudentWithTokenAsync(Factory, Admin(), cs, "s1@cu.edu.eg");
        await AccountSetup.CreateStudentWithTokenAsync(Factory, Admin(), cs, "s2@cu.edu.eg");
        await AccountSetup.CreateStudentWithTokenAsync(Factory, Admin(), cs, "s3@cu.edu.eg");
        var now = Factory.Clock.GetUtcNow();

        var page = await ActivityAsync($"actorId={AdminId}&entityType=user&page=2&pageSize=2");
        Assert.Equal(3, (int)page["totalCount"]!);
        Assert.Single(Items(page));

        Assert.Equal(0, (int)(await ActivityAsync($"actorId={Guid.CreateVersion7()}"))["totalCount"]!);
        var from = Uri.EscapeDataString(now.AddMinutes(1).ToString("O"));
        Assert.Equal(0, (int)(await ActivityAsync($"entityType=user&from={from}"))["totalCount"]!);
        var to = Uri.EscapeDataString(now.AddMinutes(1).ToString("O"));
        Assert.Equal(3, (int)(await ActivityAsync($"entityType=user&to={to}"))["totalCount"]!);
    }

    [Fact]
    public async Task Only_student_affairs_reads_the_log()
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await Factory.CreateClientAs("professor").GetAsync("/api/admin/activity")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Factory.CreateClientAs("student").GetAsync("/api/admin/activity")).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private Task<CampusWorld> BuildCampusAsync() => RegistrationSetup.BuildCampusAsync(Factory, Admin());

    private async Task<JsonObject> ActivityAsync(string query) =>
        (await Admin().GetFromJsonAsync<JsonObject>($"/api/admin/activity?{query}"))!;

    private static JsonArray Items(JsonObject page) => page["items"]!.AsArray();

    private static List<string?> Actions(JsonArray rows) => rows.Select(r => (string?)r!["action"]).ToList();
}
