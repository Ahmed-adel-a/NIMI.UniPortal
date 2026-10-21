// ACCEPTANCE TEST — written by the consultant for phase P05-payments. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Payments.PaymentsSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Payments;

/// <summary>P5.6 — Student Affairs tracking: expected vs collected per fee item.</summary>
public sealed class PaymentReportTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task The_report_compares_expected_with_collected_per_fee_item()
    {
        var w = await World();
        await RecordedAsync(Admin(), w.Ahmed, w.Tuition, 5000m);
        await RecordedAsync(Admin(), w.Mona, w.Tuition, 1000m);
        await RecordedAsync(Admin(), w.Omar, w.LabFee, 300m);

        var report = await ReportAsync($"?termId={w.TermId}");

        var tuition = Row(report, w.Tuition);
        Assert.Equal(20000m, (decimal)tuition["expected"]!);        // 4 active students × 5000
        Assert.Equal(6000m, (decimal)tuition["collected"]!);
        Assert.Equal(2, (int)tuition["payers"]!);
        Assert.Equal(14000m, (decimal)tuition["outstanding"]!);
        var lab = Row(report, w.LabFee);
        Assert.Equal(900m, (decimal)lab["expected"]!);              // 3 Engineering students × 300
        Assert.Equal(300m, (decimal)lab["collected"]!);
    }

    [Fact]
    public async Task The_report_can_be_narrowed_to_a_faculty_or_department()
    {
        var w = await World();
        await RecordedAsync(Admin(), w.Sara, w.Tuition, 5000m);

        var engineering = await ReportAsync($"?termId={w.TermId}&facultyId={w.Eng}");
        var computerScience = await ReportAsync($"?termId={w.TermId}&departmentId={w.Cs}");

        Assert.Equal(15000m, (decimal)Row(engineering, w.Tuition)["expected"]!);
        Assert.Equal(0m, (decimal)Row(engineering, w.Tuition)["collected"]!);    // Sara is in Commerce
        Assert.Equal(10000m, (decimal)Row(computerScience, w.Tuition)["expected"]!);
        Assert.Equal(600m, (decimal)Row(computerScience, w.LabFee)["expected"]!);
    }

    [Fact]
    public async Task Only_admins_see_the_report()
    {
        var w = await World();

        Assert.Equal(HttpStatusCode.Forbidden, (await w.AhmedClient.GetAsync("/api/admin/payments/report")).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private Task<PaymentsWorld> World() => BuildAsync(Factory, Admin());

    private async Task<JsonObject> ReportAsync(string query) =>
        (await Admin().GetFromJsonAsync<JsonObject>("/api/admin/payments/report" + query))!;

    private static JsonNode Row(JsonObject report, Guid feeItemId) =>
        report["items"]!.AsArray().Single(i => (Guid)i!["feeItemId"]! == feeItemId)!;
}
