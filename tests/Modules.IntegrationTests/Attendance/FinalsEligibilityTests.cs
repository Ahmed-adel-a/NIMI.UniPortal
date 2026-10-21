// ACCEPTANCE TEST — written by the consultant for phase P10. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Users;
using static NIMI.UniPortal.Modules.IntegrationTests.Attendance.AttendanceSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Attendance;

/// <summary>
/// P10.5 — finals eligibility: every confirmed member of a group with closed sessions, eligible unless their rate is below
/// the effective threshold of the subject's faculty (barred). Student Affairs sees every group, a professor only their own.
/// </summary>
public sealed class FinalsEligibilityTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task Students_below_the_threshold_are_barred_and_the_rest_eligible()
    {
        var w = await HeldLectureAsync();

        var list = await EligibilityAsync(w.HanyClient, $"groupId={w.GroupId}");

        Assert.Equal(3, (int)list["totalCount"]!);
        var mona = Item(list, w.Mona);
        Assert.False((bool)mona["eligible"]!);
        Assert.Equal(0, (int)mona["attended"]!);
        Assert.Equal(1, (int)mona["held"]!);
        Assert.Equal(0m, (decimal)mona["percentage"]!);
        Assert.Equal(75m, (decimal)mona["threshold"]!);
        Assert.Equal("CS301", (string?)mona["subjectCode"]);
        Assert.True((bool)Item(list, w.Ahmed)["eligible"]!);
        Assert.True((bool)Item(list, w.Omar)["eligible"]!);
    }

    [Fact]
    public async Task The_barred_filter_lists_only_barred_students()
    {
        var w = await HeldLectureAsync();

        var barred = await EligibilityAsync(Admin(), $"termId={w.TermId}&barredOnly=true");

        Assert.Equal([w.Mona], barred["items"]!.AsArray().Select(i => (Guid)i!["studentId"]!));
    }

    [Fact]
    public async Task The_faculty_filter_and_threshold_follow_the_subjects_faculty()
    {
        var w = await HeldLectureAsync();
        var other = await OrganizationSetup.CreateFacultyAsync(Admin(), "COM");
        var url = $"/api/admin/faculties/{w.FacultyId}/settings";
        var current = (await Admin().GetFromJsonAsync<JsonObject>(url))!;
        Assert.Equal(HttpStatusCode.OK, (await Admin().PutAsJsonAsync(url, new { attendanceThreshold = 0, version = (uint)current["version"]! })).StatusCode);

        Assert.Equal(0, (int)(await EligibilityAsync(Admin(), $"termId={w.TermId}&facultyId={other}"))["totalCount"]!);
        var mine = await EligibilityAsync(Admin(), $"termId={w.TermId}&facultyId={w.FacultyId}");
        Assert.Equal(3, (int)mine["totalCount"]!);
        Assert.All(mine["items"]!.AsArray(), i => Assert.True((bool)i!["eligible"]!));   // 0 % is not below 0 %
    }

    [Fact]
    public async Task Only_student_affairs_and_the_groups_teachers_see_eligibility()
    {
        var w = await HeldLectureAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await w.LailaClient.GetAsync($"/api/attendance/eligibility?groupId={w.GroupId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await w.AhmedClient.GetAsync("/api/attendance/eligibility")).StatusCode);
        Assert.Equal(0, (int)(await EligibilityAsync(w.LailaClient, ""))["totalCount"]!);       // teaches nothing → nothing
        Assert.Equal(3, (int)(await EligibilityAsync(w.HanyClient, ""))["totalCount"]!);        // own groups, active term
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    /// <summary>One closed lecture: Ahmed present (manual), Omar present (manual), Mona auto-absent.</summary>
    private async Task<LectureWorld> HeldLectureAsync()
    {
        var w = await BuildLectureAsync(Factory, Admin());
        var sessionId = await OpenedAsync(w.HanyClient, w.SlotId);
        Assert.Equal(HttpStatusCode.OK, (await ManualAsync(w.HanyClient, sessionId, w.Ahmed, "present", "Phone broken")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ManualAsync(w.HanyClient, sessionId, w.Omar, "present", "Scanner queue")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await CloseAsync(w.HanyClient, sessionId)).StatusCode);
        return w;
    }

    private static async Task<JsonObject> EligibilityAsync(HttpClient client, string query)
    {
        var response = await client.GetAsync($"/api/attendance/eligibility?{query}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonObject>())!;
    }

    private static JsonNode Item(JsonObject list, Guid studentId) =>
        list["items"]!.AsArray().Single(i => (Guid)i!["studentId"]! == studentId)!;
}
