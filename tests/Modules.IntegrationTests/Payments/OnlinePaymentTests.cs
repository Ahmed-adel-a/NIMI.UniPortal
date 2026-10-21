// ACCEPTANCE TEST — written by the consultant for phase P05-payments. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Payments.PaymentsSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Payments;

/// <summary>P5.7 — online payments through IPaymentGateway with the mock gateway (decision PAYGW, rules R9–R11).</summary>
public sealed class OnlinePaymentTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task Checkout_then_a_successful_mock_payment_records_an_online_payment_with_a_receipt()
    {
        var w = await World();

        var checkout = await CheckoutAsync(w.AhmedClient, w.Tuition, 1000m);
        Assert.Equal(HttpStatusCode.OK, checkout.StatusCode);
        var started = await BodyOf(checkout);
        var intentId = (Guid)started["paymentIntentId"]!;
        Assert.StartsWith($"/api/payments/mock/{intentId}", (string?)started["redirectUrl"]);
        Assert.Equal("pending", (string?)(await IntentAsync(w.AhmedClient, intentId))["status"]);

        var completed = await CompleteMockAsync(w.AhmedClient, intentId, "success");

        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        var result = await BodyOf(completed);
        Assert.Equal("succeeded", (string?)result["status"]);
        Assert.StartsWith("RCP-", (string?)result["receiptNumber"]);
        var tuition = Fee(await FeesAsync(w.AhmedClient), w.Tuition);
        Assert.Equal(1000m, (decimal)tuition["amountPaid"]!);
        Assert.Equal("partially_paid", (string?)tuition["status"]);
        var history = (await w.AhmedClient.GetFromJsonAsync<JsonObject>("/api/payments/history"))!;
        Assert.Equal("online", (string?)history["items"]![0]!["method"]);
        Assert.Equal((string?)result["receiptNumber"], (string?)history["items"]![0]!["receiptNumber"]);
    }

    [Fact]
    public async Task A_failed_payment_records_nothing_and_tells_the_student()
    {
        var w = await World();
        var intentId = await CheckedOutAsync(w.AhmedClient, w.Tuition, 1000m);

        var completed = await CompleteMockAsync(w.AhmedClient, intentId, "fail");

        var result = await BodyOf(completed);
        Assert.Equal("failed", (string?)result["status"]);
        Assert.False(string.IsNullOrWhiteSpace((string?)result["failureReason"]));
        Assert.Null(result["receiptNumber"]);
        Assert.Equal(0m, (decimal)Fee(await FeesAsync(w.AhmedClient), w.Tuition)["amountPaid"]!);
        Assert.Equal(1, (int)(await w.AhmedClient.GetFromJsonAsync<JsonObject>("/api/notifications/mine/unread-count"))!["count"]!);
    }

    [Fact]
    public async Task A_repeated_provider_callback_records_the_payment_only_once()
    {
        var w = await World();
        var intentId = await CheckedOutAsync(w.AhmedClient, w.Tuition, 1000m);
        await CompleteMockAsync(w.AhmedClient, intentId, "success");

        var replay = await PostMockCallbackAsync(Factory.CreateClient(), $"mock_{intentId}", succeeded: true, amount: 1000m);

        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(1, (int)(await w.AhmedClient.GetFromJsonAsync<JsonObject>("/api/payments/history"))!["totalCount"]!);
        Assert.Equal(1000m, (decimal)Fee(await FeesAsync(w.AhmedClient), w.Tuition)["amountPaid"]!);
    }

    [Fact]
    public async Task A_callback_with_a_bad_signature_changes_nothing()
    {
        var w = await World();
        var intentId = await CheckedOutAsync(w.AhmedClient, w.Tuition, 1000m);

        var forged = await PostMockCallbackAsync(Factory.CreateClient(), $"mock_{intentId}", succeeded: true, amount: 1000m,
            signatureOverride: new string('0', 64));
        var unknown = await PostMockCallbackAsync(Factory.CreateClient(), $"mock_{Guid.CreateVersion7()}", succeeded: true, amount: 1000m);

        Assert.Equal(HttpStatusCode.Unauthorized, forged.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal("pending", (string?)(await IntentAsync(w.AhmedClient, intentId))["status"]);
    }

    [Fact]
    public async Task An_intent_expires_after_30_minutes()
    {
        var w = await World();
        var intentId = await CheckedOutAsync(w.AhmedClient, w.Tuition, 1000m);

        Factory.Clock.Advance(TimeSpan.FromMinutes(31));
        var completed = await CompleteMockAsync(w.AhmedClient, intentId, "success");

        Assert.Equal("expired", (string?)(await BodyOf(completed))["status"]);
        Assert.Equal(0m, (decimal)Fee(await FeesAsync(w.AhmedClient), w.Tuition)["amountPaid"]!);
    }

    [Fact]
    public async Task Checkout_follows_the_same_rules_as_office_payments()
    {
        var w = await World();

        var aboveBalance = await CheckoutAsync(w.AhmedClient, w.LabFee, 300.01m);
        var notMine = await CheckoutAsync(w.SaraClient, w.LabFee, 100m);
        await CheckedOutAsync(w.AhmedClient, w.Tuition, 1000m);
        var secondPending = await CheckoutAsync(w.AhmedClient, w.Tuition, 500m);

        Assert.Equal(HttpStatusCode.BadRequest, aboveBalance.StatusCode);
        Assert.Contains("amount", await FieldsOf(aboveBalance));
        Assert.Equal(HttpStatusCode.BadRequest, notMine.StatusCode);
        Assert.Contains("feeItemId", await FieldsOf(notMine));
        Assert.Equal(HttpStatusCode.Conflict, secondPending.StatusCode);
    }

    [Fact]
    public async Task A_student_cannot_see_or_complete_someone_elses_intent()
    {
        var w = await World();
        var intentId = await CheckedOutAsync(w.AhmedClient, w.Tuition, 1000m);

        Assert.Equal(HttpStatusCode.NotFound, (await w.MonaClient.GetAsync($"/api/payments/intents/{intentId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await CompleteMockAsync(w.MonaClient, intentId, "success")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await CheckoutAsync(Factory.CreateClientAs("professor"), w.Tuition, 10m)).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private Task<PaymentsWorld> World() => BuildAsync(Factory, Admin());

    private static async Task<JsonObject> IntentAsync(HttpClient student, Guid intentId) =>
        (await student.GetFromJsonAsync<JsonObject>($"/api/payments/intents/{intentId}"))!;
}
