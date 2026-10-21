using System.Net;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Payments.PaymentsSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Payments;

/// <summary>
/// Implementer extra test (not an acceptance file): P5.8 creates only a student-fee row for a military-education
/// item, so both the office and online payment paths must treat that row as payable even though the item never
/// applies by targeting.
/// </summary>
public sealed class MilitaryFeePaymentTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task An_event_created_military_fee_can_be_paid_by_the_office_and_online()
    {
        var w = await World();
        var fee = await CreateFeeItemAsync(
            Admin(), "Military education", "military_education", 500m, w.TermId, nameAr: "التربية العسكرية");

        await Factory.PublishAsync(new MilitaryEducationRequested(
            Guid.CreateVersion7(),
            Factory.Clock.GetUtcNow(),
            w.Ahmed,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            w.TermId));

        var due = Fee(await FeesAsync(w.AhmedClient), fee);
        Assert.Equal(500m, (decimal)due["amountDue"]!);
        Assert.Equal("military_education", (string?)due["category"]);

        var first = await RecordedAsync(Admin(), w.Ahmed, fee, 200m);
        Assert.Equal("partially_paid", (string?)first["feeStatus"]);

        var intentId = await CheckedOutAsync(w.AhmedClient, fee, 300m);
        var completed = await CompleteMockAsync(w.AhmedClient, intentId, "success");
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        Assert.Equal("succeeded", (string?)(await BodyOf(completed))["status"]);

        var paid = Fee(await FeesAsync(w.AhmedClient), fee);
        Assert.Equal(0m, (decimal)paid["balance"]!);
        Assert.Equal("paid", (string?)paid["status"]);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private Task<PaymentsWorld> World() => BuildAsync(Factory, Admin());
}
