// ACCEPTANCE TEST — written by the consultant for phase P10. Do not modify; report problems instead.
using System.Net;
using System.Text;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Lifecycle.RegistrationSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Lifecycle;

/// <summary>P10.4 — the student's schedule as an Excel-friendly CSV (UTF-8 with BOM), same range rules as GET /api/schedule.</summary>
public sealed class ScheduleExportTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task The_student_downloads_their_week_as_csv()
    {
        var w = await BuildCampusAsync(Factory, Factory.CreateClientAs("admin"));
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);                 // Sunday 09:00–10:30, Hall A

        var response = await w.AhmedClient.GetAsync("/api/schedule/export?from=2026-09-19&to=2026-09-25");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains(".csv", response.Content.Headers.ContentDisposition?.FileName ?? response.Content.Headers.ContentDisposition?.FileNameStar);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);
        var lines = Encoding.UTF8.GetString(bytes[3..]).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);                                       // header + the one Sunday lecture
        Assert.StartsWith("Date,", lines[0]);
        Assert.Contains("2026-09-20", lines[1]);
        Assert.Contains("CS301", lines[1]);
        Assert.Contains("الخوارزميات", lines[1]);
        Assert.Contains("09:00", lines[1]);
        Assert.Contains("Hall A", lines[1]);
    }

    [Fact]
    public async Task The_export_keeps_the_schedule_range_rules()
    {
        var w = await BuildCampusAsync(Factory, Factory.CreateClientAs("admin"));

        var tooLong = await w.AhmedClient.GetAsync("/api/schedule/export?from=2026-09-19&to=2026-11-30");

        Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
        Assert.Contains("to", await FieldsOf(tooLong));
        Assert.Equal(HttpStatusCode.Forbidden, (await w.HanyClient.GetAsync("/api/schedule/export?from=2026-09-19&to=2026-09-25")).StatusCode);
    }
}
