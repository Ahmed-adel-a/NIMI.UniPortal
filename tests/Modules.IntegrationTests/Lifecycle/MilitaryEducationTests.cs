// ACCEPTANCE TEST — written by the consultant for phase P05-military. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Lifecycle.RegistrationSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Payments.PaymentsSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Lifecycle;

/// <summary>
/// P5.5 — military education (MILEDU, D5): apply or be assigned → the fee appears → paying it in full confirms the
/// registration. Lifecycle and Payments talk only through events. Campus: see <see cref="CampusWorld"/> (MIL100 group 1).
/// </summary>
public sealed class MilitaryEducationTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task Applying_creates_a_pending_registration_and_a_fee_to_pay()
    {
        var w = await Campus();
        var fee = await MilitaryFeeAsync(w);

        var response = await ApplyAsync(w.AhmedClient, w.Mil100Group1);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var applied = await BodyOf(response);
        Assert.Equal("pending_payment", (string?)applied["status"]);
        Assert.Equal("MIL100", (string?)applied["subjectCode"]);
        Assert.Equal(w.Mil100Group1, (Guid)applied["groupId"]!);
        Assert.Equal("pending_payment", (string?)(await MineAsync(w.AhmedClient))["registrations"]![0]!["status"]);
        Assert.Empty(await RosterAsync(w.Mil100Group1));                                   // not confirmed yet (R11)
        var due = Fee(await FeesAsync(w.AhmedClient), fee);
        Assert.Equal(500m, (decimal)due["amountDue"]!);
        Assert.Equal("military_education", (string?)due["category"]);
        Assert.DoesNotContain(
            (await FeesAsync(w.MonaClient))["fees"]!.AsArray(), f => (Guid)f!["feeItemId"]! == fee);
        Assert.Equal(1, await UnreadAsync(w.AhmedClient));                                 // "military-education fee due"
    }

    [Fact]
    public async Task The_registration_is_confirmed_only_when_the_fee_is_fully_paid()
    {
        var w = await Campus();
        var fee = await MilitaryFeeAsync(w);
        await ApplyAsync(w.AhmedClient, w.Mil100Group1);

        await RecordedAsync(Admin(), w.Ahmed, fee, 200m);
        var afterFirst = await RosterAsync(w.Mil100Group1);
        await RecordedAsync(Admin(), w.Ahmed, fee, 300m);
        var afterSecond = await RosterAsync(w.Mil100Group1);

        Assert.Empty(afterFirst);
        Assert.Equal([w.Ahmed], afterSecond);
        Assert.Equal("registered", (string?)(await MineAsync(w.AhmedClient))["registrations"]![0]!["status"]);
    }

    [Fact]
    public async Task Paying_the_fee_online_confirms_the_registration_too()
    {
        var w = await Campus();
        var fee = await MilitaryFeeAsync(w);
        await ApplyAsync(w.AhmedClient, w.Mil100Group1);

        var intentId = await CheckedOutAsync(w.AhmedClient, fee, 500m);
        await CompleteMockAsync(w.AhmedClient, intentId, "success");

        Assert.Equal([w.Ahmed], await RosterAsync(w.Mil100Group1));
    }

    [Fact]
    public async Task In_mandatory_mode_students_cannot_apply_and_Student_Affairs_assigns_them()
    {
        var w = await Campus();
        var fee = await MilitaryFeeAsync(w);
        var mode = await Admin().PutAsJsonAsync($"/api/admin/faculties/{w.FacultyId}/settings",
            new { militaryEducationPolicy = new { mode = "mandatory" } });
        Assert.Equal(HttpStatusCode.OK, mode.StatusCode);

        var apply = await ApplyAsync(w.AhmedClient, w.Mil100Group1);
        var assign = await AssignAsync([w.Ahmed, w.Mona, w.Hany], w.Mil100Group1);
        var again = await AssignAsync([w.Ahmed], w.Mil100Group1);

        Assert.Equal(HttpStatusCode.BadRequest, apply.StatusCode);
        Assert.Contains("subjectGroupId", await FieldsOf(apply));
        Assert.Equal(
            ["assigned", "assigned", "not_a_student"],
            new[] { w.Ahmed, w.Mona, w.Hany }.Select(id => Outcome(assign, id)));
        Assert.Equal("already_registered", Outcome(again, w.Ahmed));
        Assert.Equal(500m, (decimal)Fee(await FeesAsync(w.MonaClient), fee)["amountDue"]!);
    }

    [Fact]
    public async Task Only_military_education_groups_once_and_only_by_students()
    {
        var w = await Campus();
        await MilitaryFeeAsync(w);
        await ApplyAsync(w.AhmedClient, w.Mil100Group1);

        var regularGroup = await ApplyAsync(w.MonaClient, w.Cs301Group1);
        var twice = await ApplyAsync(w.AhmedClient, w.Mil100Group1);
        var professor = await ApplyAsync(w.HanyClient, w.Mil100Group1);
        var notAdmin = await w.AhmedClient.PostAsJsonAsync("/api/admin/military-education/assign",
            new { studentIds = new[] { w.Mona }, subjectGroupId = w.Mil100Group1 });

        Assert.Equal(HttpStatusCode.BadRequest, regularGroup.StatusCode);
        Assert.Contains("subjectGroupId", await FieldsOf(regularGroup));
        Assert.Equal(HttpStatusCode.Conflict, twice.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, professor.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, notAdmin.StatusCode);
    }

    [Fact]
    public async Task Resync_creates_missing_fees_after_the_fee_item_is_defined_and_is_safe_to_repeat()
    {
        var w = await Campus();
        await ApplyAsync(w.AhmedClient, w.Mil100Group1);                                   // no fee item yet
        Assert.Empty(MilitaryFees(await FeesAsync(w.AhmedClient)));

        var fee = await MilitaryFeeAsync(w);
        var first = await ResyncAsync(w.TermId);
        var second = await ResyncAsync(w.TermId);

        Assert.Equal(1, (int)first["republished"]!);
        Assert.Equal(1, (int)second["republished"]!);
        Assert.Equal([fee], MilitaryFees(await FeesAsync(w.AhmedClient)));                 // still exactly one
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private Task<CampusWorld> Campus() => BuildCampusAsync(Factory, Admin());

    private Task<Guid> MilitaryFeeAsync(CampusWorld w) =>
        CreateFeeItemAsync(Admin(), "Military education", "military_education", 500m, w.TermId, nameAr: "التربية العسكرية");

    private static async Task<HttpResponseMessage> ApplyAsync(HttpClient client, Guid subjectGroupId) =>
        await client.PostAsJsonAsync("/api/military-education/apply", new { subjectGroupId });

    private async Task<JsonObject> AssignAsync(Guid[] studentIds, Guid subjectGroupId)
    {
        var response = await Admin().PostAsJsonAsync("/api/admin/military-education/assign", new { studentIds, subjectGroupId });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await BodyOf(response);
    }

    private async Task<JsonObject> ResyncAsync(Guid termId)
    {
        var response = await Admin().PostAsync($"/api/admin/military-education/resync?termId={termId}", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await BodyOf(response);
    }

    private async Task<List<Guid>> RosterAsync(Guid groupId) =>
        (await Admin().GetFromJsonAsync<JsonObject>($"/api/admin/groups/{groupId}/roster"))!["items"]!
            .AsArray().Select(s => (Guid)s!["studentId"]!).ToList();

    private static async Task<int> UnreadAsync(HttpClient client) =>
        (int)(await client.GetFromJsonAsync<JsonObject>("/api/notifications/mine/unread-count"))!["count"]!;

    private static string? Outcome(JsonObject assign, Guid studentId) =>
        (string?)assign["results"]!.AsArray().Single(r => (Guid)r!["studentId"]! == studentId)!["outcome"];

    private static List<Guid> MilitaryFees(JsonObject fees) =>
        fees["fees"]!.AsArray()
            .Where(f => (string?)f!["category"] == "military_education")
            .Select(f => (Guid)f!["feeItemId"]!)
            .ToList();
}
