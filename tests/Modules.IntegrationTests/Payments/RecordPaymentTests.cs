// ACCEPTANCE TEST — written by the consultant for phase P05-payments. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Payments.PaymentsSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Payments;

/// <summary>P5.3 + P5.4 — office payments, receipts and the student's full payment history (PAYHIST, rules R1–R5).</summary>
public sealed class RecordPaymentTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task Admin_records_a_partial_then_a_full_payment_with_receipts()
    {
        var w = await World();

        var first = await RecordPaymentAsync(Admin(), w.Ahmed, w.Tuition, 2000m, "bank_transfer", "TRX-889");
        var second = await RecordPaymentAsync(Admin(), w.Ahmed, w.Tuition, 3000m);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var partial = await BodyOf(first);
        Assert.Matches(new Regex(@"^RCP-\d{4}-\d{6}$"), (string)partial["receiptNumber"]!);
        Assert.Equal(2000m, (decimal)partial["amount"]!);
        Assert.Equal("bank_transfer", (string?)partial["method"]);
        Assert.Equal("TRX-889", (string?)partial["reference"]);
        Assert.Equal("partially_paid", (string?)partial["feeStatus"]);
        Assert.Equal(3000m, (decimal)partial["balance"]!);
        var full = await BodyOf(second);
        Assert.Equal("paid", (string?)full["feeStatus"]);
        Assert.Equal(0m, (decimal)full["balance"]!);
        Assert.Equal("paid", (string?)Fee(await FeesAsync(w.AhmedClient), w.Tuition)["status"]);
    }

    [Fact]
    public async Task Receipt_numbers_are_unique_and_sequential()
    {
        var w = await World();

        var numbers = new List<string>();
        foreach (var (student, fee) in new[] { (w.Ahmed, w.Tuition), (w.Mona, w.Tuition), (w.Omar, w.LabFee) })
            numbers.Add((string)(await RecordedAsync(Admin(), student, fee, 100m))["receiptNumber"]!);

        var sequences = numbers.Select(n => int.Parse(n[^6..])).ToList();
        Assert.Equal(sequences[0] + 1, sequences[1]);
        Assert.Equal(sequences[1] + 1, sequences[2]);
        Assert.Single(numbers.Select(n => n[..8]).Distinct());              // same "RCP-yyyy" prefix
    }

    [Fact]
    public async Task Paying_more_than_the_balance_is_a_400_and_changes_nothing()
    {
        var w = await World();
        await RecordedAsync(Admin(), w.Ahmed, w.LabFee, 200m);

        var response = await RecordPaymentAsync(Admin(), w.Ahmed, w.LabFee, 100.01m);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("amount", await FieldsOf(response));
        Assert.Equal(100m, (decimal)Fee(await FeesAsync(w.AhmedClient), w.LabFee)["balance"]!);
    }

    [Fact]
    public async Task Invalid_payments_are_a_400_naming_the_field()
    {
        var w = await World();

        var notApplicable = await RecordPaymentAsync(Admin(), w.Sara, w.LabFee, 100m);        // Commerce student, Engineering fee
        var notAStudent = await RecordPaymentAsync(Admin(), Guid.CreateVersion7(), w.Tuition, 100m);
        var online = await RecordPaymentAsync(Admin(), w.Ahmed, w.Tuition, 100m, "online");
        var zero = await RecordPaymentAsync(Admin(), w.Ahmed, w.Tuition, 0m);

        Assert.Contains("feeItemId", await FieldsOf(notApplicable));
        Assert.Contains("studentId", await FieldsOf(notAStudent));
        Assert.Contains("method", await FieldsOf(online));
        Assert.Contains("amount", await FieldsOf(zero));
    }

    [Fact]
    public async Task Two_simultaneous_payments_can_never_overpay_a_fee()
    {
        var w = await World();

        var responses = await Task.WhenAll(
            RecordPaymentAsync(Admin(), w.Ahmed, w.LabFee, 200m),
            RecordPaymentAsync(Admin(), w.Ahmed, w.LabFee, 200m));

        var lab = Fee(await FeesAsync(w.AhmedClient), w.LabFee);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, r => r.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.BadRequest);
        Assert.Equal(200m, (decimal)lab["amountPaid"]!);
    }

    [Fact]
    public async Task A_student_sees_every_payment_newest_first_and_only_their_own()
    {
        var w = await World();
        await RecordedAsync(Admin(), w.Ahmed, w.LabFee, 300m);
        Factory.Clock.Advance(TimeSpan.FromMinutes(5));
        await RecordedAsync(Admin(), w.Ahmed, w.Tuition, 1000m);
        Factory.Clock.Advance(TimeSpan.FromMinutes(5));
        await RecordedAsync(Admin(), w.Mona, w.Tuition, 1000m);

        var history = (await w.AhmedClient.GetFromJsonAsync<JsonObject>("/api/payments/history"))!;

        Assert.Equal(2, (int)history["totalCount"]!);
        var items = history["items"]!.AsArray();
        Assert.Equal(["Tuition", "Lab fee"], items.Select(p => (string?)p!["feeName"]));
        Assert.Equal("المصروفات الدراسية", (string?)items[0]!["feeNameAr"]);
        Assert.Equal("cash", (string?)items[0]!["method"]);
    }

    [Fact]
    public async Task A_receipt_is_visible_to_its_student_and_admins_only()
    {
        var w = await World();
        var payment = await RecordedAsync(Admin(), w.Ahmed, w.Tuition, 1000m, "bank_transfer");
        var id = (Guid)payment["id"]!;

        var own = (await w.AhmedClient.GetFromJsonAsync<JsonObject>($"/api/payments/{id}/receipt"))!;
        var asAdmin = await Admin().GetAsync($"/api/payments/{id}/receipt");
        var someoneElse = await w.MonaClient.GetAsync($"/api/payments/{id}/receipt");

        Assert.Equal((string?)payment["receiptNumber"], (string?)own["receiptNumber"]);
        Assert.Equal(w.Ahmed, (Guid)own["studentId"]!);
        Assert.Equal("Test Student", (string?)own["studentName"]);
        Assert.NotNull((string?)own["studentCode"]);
        Assert.Equal("Tuition", (string?)own["feeName"]);
        Assert.Equal(1000m, (decimal)own["amount"]!);
        Assert.Equal("bank_transfer", (string?)own["method"]);
        Assert.Equal(HttpStatusCode.OK, asAdmin.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, someoneElse.StatusCode);
    }

    [Fact]
    public async Task Admin_lists_payments_by_student()
    {
        var w = await World();
        await RecordedAsync(Admin(), w.Ahmed, w.Tuition, 1000m);
        await RecordedAsync(Admin(), w.Mona, w.Tuition, 1000m);

        var list = (await Admin().GetFromJsonAsync<JsonObject>($"/api/admin/payments?studentId={w.Mona}"))!;

        Assert.Equal(1, (int)list["totalCount"]!);
        Assert.Equal(HttpStatusCode.Forbidden, (await w.AhmedClient.GetAsync("/api/admin/payments")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await RecordPaymentAsync(w.AhmedClient, w.Ahmed, w.Tuition, 10m)).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private Task<PaymentsWorld> World() => BuildAsync(Factory, Admin());
}
