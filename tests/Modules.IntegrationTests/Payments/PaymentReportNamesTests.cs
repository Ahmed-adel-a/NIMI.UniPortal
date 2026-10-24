using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Payments.PaymentsSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Payments;

/// <summary>The report names each fee item in both languages, so the web app can show it entirely in one.</summary>
public sealed class PaymentReportNamesTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task Each_report_row_carries_the_english_and_arabic_name()
    {
        var admin = Factory.CreateClientAs("admin");
        var w = await BuildAsync(Factory, admin);

        var report = (await admin.GetFromJsonAsync<JsonObject>($"/api/admin/payments/report?termId={w.TermId}"))!;
        var tuition = report["items"]!.AsArray().Single(i => (Guid)i!["feeItemId"]! == w.Tuition)!;

        Assert.Equal("Tuition", (string)tuition["name"]!);
        Assert.Equal("المصروفات الدراسية", (string)tuition["nameAr"]!);
    }
}
