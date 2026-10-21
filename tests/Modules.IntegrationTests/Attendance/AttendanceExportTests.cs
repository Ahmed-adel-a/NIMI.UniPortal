// ACCEPTANCE TEST — written by the consultant for phase P10. Do not modify; report problems instead.
using System.Net;
using System.Text;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Attendance.AttendanceSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Attendance;

/// <summary>P10.4 — a group's attendance sheet as CSV (UTF-8 with BOM) for its teaching staff and Student Affairs.</summary>
public sealed class AttendanceExportTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task The_teacher_downloads_the_groups_attendance_sheet()
    {
        var w = await BuildLectureAsync(Factory, Admin());
        var sessionId = await OpenedAsync(w.HanyClient, w.SlotId);
        Assert.Equal(HttpStatusCode.OK, (await ManualAsync(w.HanyClient, sessionId, w.Ahmed, "present", "Phone broken")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await CloseAsync(w.HanyClient, sessionId)).StatusCode);

        var response = await w.HanyClient.GetAsync($"/api/attendance/groups/{w.GroupId}/export");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);
        var lines = Encoding.UTF8.GetString(bytes[3..]).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(4, lines.Length);                                                  // header + Ahmed, Mona, Omar
        Assert.StartsWith("Student code,Name,Attended,Held,Percentage,", lines[0]);
        Assert.EndsWith(w.Today.ToString("yyyy-MM-dd"), lines[0]);                      // one column per closed session
        Assert.Single(lines, l => l.Contains(",1,1,100.00,manual_audit"));              // Ahmed
        Assert.Equal(2, lines.Count(l => l.Contains(",0,1,0.00,absent")));              // Mona, Omar (auto-absent at close)
    }

    [Fact]
    public async Task Only_the_groups_staff_and_student_affairs_export()
    {
        var w = await BuildLectureAsync(Factory, Admin());

        Assert.Equal(HttpStatusCode.OK, (await Admin().GetAsync($"/api/attendance/groups/{w.GroupId}/export")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await w.LailaClient.GetAsync($"/api/attendance/groups/{w.GroupId}/export")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await w.AhmedClient.GetAsync($"/api/attendance/groups/{w.GroupId}/export")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Admin().GetAsync($"/api/attendance/groups/{Guid.CreateVersion7()}/export")).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");
}
