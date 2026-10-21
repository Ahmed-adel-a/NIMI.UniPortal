// ACCEPTANCE TEST — written by the consultant for phase P03. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Scheduling;

/// <summary>P3.1 — academic terms (rules R1, R2).</summary>
public sealed class TermTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task Admin_creates_a_planned_term()
    {
        var response = await PostTermAsync(Admin());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var term = await BodyOf(response);
        Assert.Equal(2026, (int)term["academicYear"]!);
        Assert.Equal("first", (string?)term["number"]);
        Assert.Equal(TermStart, (string?)term["startsOn"]);
        Assert.Equal(TermEnd, (string?)term["endsOn"]);
        Assert.Equal(17, (int)term["weekCount"]!);
        Assert.Equal("planned", (string?)term["status"]);
        var listed = (await Student().GetFromJsonAsync<JsonObject>("/api/terms"))!;
        Assert.Equal(1, (int)listed["totalCount"]!);
    }

    [Fact]
    public async Task There_is_no_current_term_until_one_is_activated()
    {
        var termId = await CreateTermAsync(Admin());

        var before = await Student().GetAsync("/api/terms/current");
        var activated = await Admin().PostAsync($"/api/admin/terms/{termId}/activate", null);
        var current = (await Student().GetFromJsonAsync<JsonObject>("/api/terms/current"))!;

        Assert.Equal(HttpStatusCode.NotFound, before.StatusCode);
        Assert.Equal(HttpStatusCode.OK, activated.StatusCode);
        Assert.Equal("active", (string?)(await BodyOf(activated))["status"]);
        Assert.Equal(termId, (Guid)current["id"]!);
    }

    [Fact]
    public async Task Activating_a_term_closes_the_previously_active_one()
    {
        var first = await CreateTermAsync(Admin());
        var second = await CreateTermAsync(Admin(), number: "second", startsOn: "2027-02-06", endsOn: "2027-06-04", weekCount: 17);
        await Admin().PostAsync($"/api/admin/terms/{first}/activate", null);

        var response = await Admin().PostAsync($"/api/admin/terms/{second}/activate", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var terms = (await Student().GetFromJsonAsync<JsonObject>("/api/terms"))!["items"]!.AsArray();
        Assert.Equal("closed", (string?)terms.Single(t => (Guid)t!["id"]! == first)!["status"]);
        Assert.Equal("active", (string?)terms.Single(t => (Guid)t!["id"]! == second)!["status"]);
        Assert.Equal(second, (Guid)(await Student().GetFromJsonAsync<JsonObject>("/api/terms/current"))!["id"]!);
    }

    [Fact]
    public async Task Activating_an_older_term_while_a_newer_one_is_active_also_works()
    {
        // Created first = smaller Guid v7. The swap must not depend on the order the database applies the two updates.
        var older = await CreateTermAsync(Admin(), number: "summer", startsOn: "2026-07-04", endsOn: "2026-08-28", weekCount: 8);
        var newer = await CreateTermAsync(Admin());
        await Admin().PostAsync($"/api/admin/terms/{newer}/activate", null);

        var response = await Admin().PostAsync($"/api/admin/terms/{older}/activate", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(older, (Guid)(await Student().GetFromJsonAsync<JsonObject>("/api/terms/current"))!["id"]!);
        var terms = (await Student().GetFromJsonAsync<JsonObject>("/api/terms"))!["items"]!.AsArray();
        Assert.Equal("closed", (string?)terms.Single(t => (Guid)t!["id"]! == newer)!["status"]);
    }

    [Fact]
    public async Task A_planned_term_can_be_edited_but_an_active_one_cannot()
    {
        var termId = await CreateTermAsync(Admin());
        var edit = new { academicYear = 2026, number = "first", startsOn = TermStart, endsOn = TermEnd, weekCount = 16 };

        var whilePlanned = await Admin().PutAsJsonAsync($"/api/admin/terms/{termId}", edit);
        await Admin().PostAsync($"/api/admin/terms/{termId}/activate", null);
        var whileActive = await Admin().PutAsJsonAsync($"/api/admin/terms/{termId}", edit);

        Assert.Equal(HttpStatusCode.OK, whilePlanned.StatusCode);
        Assert.Equal(16, (int)(await BodyOf(whilePlanned))["weekCount"]!);
        Assert.Equal(HttpStatusCode.Conflict, whileActive.StatusCode);
        Assert.Equal("conflict", (string?)(await BodyOf(whileActive))["code"]);
    }

    [Fact]
    public async Task A_term_is_closed_explicitly()
    {
        var termId = await CreateActiveTermAsync(Admin());

        var response = await Admin().PostAsync($"/api/admin/terms/{termId}/close", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("closed", (string?)(await BodyOf(response))["status"]);
        Assert.Equal(HttpStatusCode.NotFound, (await Student().GetAsync("/api/terms/current")).StatusCode);
    }

    [Fact]
    public async Task Invalid_terms_are_a_400_naming_the_field()
    {
        var endsBeforeStart = await PostTermAsync(Admin(), startsOn: "2026-09-19", endsOn: "2026-09-01");
        var tooManyWeeks = await PostTermAsync(Admin(), weekCount: 31);

        Assert.Equal(HttpStatusCode.BadRequest, endsBeforeStart.StatusCode);
        Assert.Contains("endsOn", await FieldsOf(endsBeforeStart));
        Assert.Equal(HttpStatusCode.BadRequest, tooManyWeeks.StatusCode);
        Assert.Contains("weekCount", await FieldsOf(tooManyWeeks));
    }

    [Fact]
    public async Task The_same_year_and_number_twice_is_a_409()
    {
        await CreateTermAsync(Admin());

        var duplicate = await PostTermAsync(Admin());

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Terms_are_listed_newest_first()
    {
        await CreateTermAsync(Admin(), academicYear: 2025, startsOn: "2025-09-20", endsOn: "2026-01-16", weekCount: 17);
        await CreateTermAsync(Admin());

        var items = (await Student().GetFromJsonAsync<JsonObject>("/api/terms"))!["items"]!.AsArray();

        Assert.Equal([2026, 2025], items.Select(t => (int)t!["academicYear"]!));
    }

    [Fact]
    public async Task Unknown_term_is_a_404()
    {
        var response = await Admin().PostAsync($"/api/admin/terms/{Guid.CreateVersion7()}/activate", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Only_admins_manage_terms_but_every_signed_in_user_reads_them()
    {
        var body = new { academicYear = 2026, number = "first", startsOn = TermStart, endsOn = TermEnd, weekCount = 17 };

        Assert.Equal(HttpStatusCode.Forbidden, (await Factory.CreateClientAs("professor").PostAsJsonAsync("/api/admin/terms", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Student().PostAsJsonAsync("/api/admin/terms", body)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Factory.CreateClientAs("professor").GetAsync("/api/terms")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Factory.CreateClient().GetAsync("/api/terms")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Factory.CreateClient().PostAsJsonAsync("/api/admin/terms", body)).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private HttpClient Student() => Factory.CreateClientAs("student");
}
