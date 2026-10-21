// ACCEPTANCE TEST — written by the consultant for phase P10. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Attendance;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Attendance.AttendanceSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Grades.GradesSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Lifecycle.RegistrationSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Payments.PaymentsSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Dashboards;

/// <summary>
/// P10.6 SADASH — a self-describing metric catalog and one summary call with every metric for a term, optionally
/// narrowed to a faculty or department (students' placement). Drill-downs point at the existing paged lists.
/// </summary>
public sealed class StudentAffairsDashboardTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    private static readonly string[] Keys =
    [
        "activeStudents", "registrations", "averageAttendance", "barredStudents", "feesCollected", "feesOutstanding",
        "gradesAwaitingConfirmation", "gradesAwaitingPublication", "graduationBlocked",
    ];

    [Fact]
    public async Task The_catalog_describes_every_metric()
    {
        var catalog = (await Admin().GetFromJsonAsync<JsonArray>("/api/admin/dashboard/metrics"))!;

        Assert.Equal(Keys, catalog.Select(m => (string)m!["key"]!));
        Assert.All(catalog, m =>
        {
            Assert.False(string.IsNullOrWhiteSpace((string?)m!["title"]));
            Assert.False(string.IsNullOrWhiteSpace((string?)m["titleAr"]));
            Assert.Contains((string?)m["unit"], new[] { "count", "percent", "currency" });
        });
        Assert.Equal("/api/attendance/eligibility", (string?)catalog.Single(m => (string?)m!["key"] == "barredStudents")!["drillDownUrl"]);
    }

    [Fact]
    public async Task The_summary_counts_students_registrations_fees_grades_and_graduation()
    {
        var w = await GradedCampusAsync(Factory, Admin());                                  // Ahmed + Mona in CS301 group 1
        await AssignTeacherAsync(Admin(), w.Hany, w.Cs303, w.TermId);
        await RegisteredAsync(w.AhmedClient, w.Cs303Group1);
        await DraftedAsync(w.HanyClient, w.Cs301Group1, (w.Ahmed, 90m), (w.Mona, 70m));
        await ConfirmedAsync(Admin(), w.Cs301Group1);                                       // 2 awaiting publication
        await DraftedAsync(w.HanyClient, w.Cs303Group1, (w.Ahmed, 80m));                   // 1 awaiting confirmation
        var tuition = await CreateFeeItemAsync(Admin(), "Tuition", "tuition", 500m, w.TermId);
        await RecordedAsync(Admin(), w.Ahmed, tuition, 200m);

        var summary = await SummaryAsync($"termId={w.TermId}");

        Assert.Equal(w.TermId, (Guid)summary["termId"]!);
        Assert.Empty(summary["unavailable"]!.AsArray());
        var values = summary["values"]!;
        Assert.Equal(2m, (decimal)values["activeStudents"]!);
        Assert.Equal(3m, (decimal)values["registrations"]!);
        Assert.Null(values["averageAttendance"]);                                          // no closed session yet
        Assert.Equal(0m, (decimal)values["barredStudents"]!);
        Assert.Equal(200m, (decimal)values["feesCollected"]!);
        Assert.Equal(800m, (decimal)values["feesOutstanding"]!);
        Assert.Equal(1m, (decimal)values["gradesAwaitingConfirmation"]!);
        Assert.Equal(2m, (decimal)values["gradesAwaitingPublication"]!);
        Assert.Equal(2m, (decimal)values["graduationBlocked"]!);                           // no requirement configured
    }

    [Fact]
    public async Task A_department_filter_counts_only_that_departments_students()
    {
        var w = await GradedCampusAsync(Factory, Admin());
        await DraftedAsync(w.HanyClient, w.Cs301Group1, (w.Ahmed, 90m));
        var tuition = await CreateFeeItemAsync(Admin(), "Tuition", "tuition", 500m, w.TermId);
        await RecordedAsync(Admin(), w.Ahmed, tuition, 200m);

        var civil = (await SummaryAsync($"termId={w.TermId}&departmentId={w.CivDepartmentId}"))["values"]!;
        var cs = (await SummaryAsync($"termId={w.TermId}&facultyId={w.FacultyId}&departmentId={w.CsDepartmentId}"))["values"]!;

        foreach (var key in new[] { "activeStudents", "registrations", "feesCollected", "feesOutstanding", "gradesAwaitingConfirmation", "graduationBlocked" })
            Assert.Equal(0m, (decimal)civil[key]!);
        Assert.Equal(2m, (decimal)cs["activeStudents"]!);
        Assert.Equal(1m, (decimal)cs["gradesAwaitingConfirmation"]!);
    }

    [Fact]
    public async Task Attendance_metrics_cover_closed_sessions()
    {
        var w = await BuildLectureAsync(Factory, Admin());
        var sessionId = await OpenedAsync(w.HanyClient, w.SlotId);
        Assert.Equal(HttpStatusCode.OK, (await ManualAsync(w.HanyClient, sessionId, w.Ahmed, "present", "Phone broken")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ManualAsync(w.HanyClient, sessionId, w.Omar, "present", "Scanner queue")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await CloseAsync(w.HanyClient, sessionId)).StatusCode);   // Mona absent

        var values = (await SummaryAsync(""))["values"]!;                                   // the active term by default

        Assert.Equal(66.67m, (decimal)values["averageAttendance"]!);
        Assert.Equal(1m, (decimal)values["barredStudents"]!);
    }

    [Fact]
    public async Task Only_student_affairs_sees_the_dashboard()
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await Factory.CreateClientAs("professor").GetAsync("/api/admin/dashboard/summary")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Factory.CreateClientAs("student").GetAsync("/api/admin/dashboard/metrics")).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private async Task<JsonObject> SummaryAsync(string query)
    {
        var response = await Admin().GetAsync($"/api/admin/dashboard/summary?{query}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonObject>())!;
    }
}
