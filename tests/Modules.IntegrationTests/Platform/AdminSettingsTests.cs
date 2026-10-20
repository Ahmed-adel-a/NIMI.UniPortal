using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;

namespace NIMI.UniPortal.Modules.IntegrationTests.Platform;

/// <summary>P0.2 — GET/PUT /api/admin/settings (Student Affairs only).</summary>
public sealed class AdminSettingsTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    private const string Url = "/api/admin/settings";

    [Fact]
    public async Task Admin_reads_every_setting_with_a_version()
    {
        var settings = await GetAsAdmin();

        Assert.Equal("University", (string?)settings["universityName"]);
        Assert.Equal(75m, (decimal?)settings["attendanceThreshold"]);
        Assert.Equal("{DEPT}-{YEAR}-{SEQ:0000}", (string?)settings["studentCodeFormat"]);
        Assert.Equal("opt_in", (string?)settings["militaryEducationPolicy"]!["mode"]);
        Assert.Equal(12, settings["gradingScale"]!.AsArray().Count);
        Assert.NotEqual(0u, (uint?)settings["version"]);
    }

    [Theory]
    [InlineData("professor")]
    [InlineData("student")]
    public async Task Non_admins_are_forbidden(string role)
    {
        var client = Factory.CreateClientAs(role);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(Url)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync(Url, await GetAsAdmin())).StatusCode);
    }

    [Fact]
    public async Task Anonymous_callers_are_unauthorized()
    {
        var client = Factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(Url)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PutAsJsonAsync(Url, await GetAsAdmin())).StatusCode);
    }

    [Fact]
    public async Task Admin_update_is_saved_and_shows_in_the_public_settings()
    {
        var settings = await GetAsAdmin();
        var before = (uint)settings["version"]!;
        settings["universityName"] = "Cairo University";
        settings["universityNameAr"] = "جامعة القاهرة";
        settings["attendanceThreshold"] = 60;
        settings["workingDays"] = new JsonArray(1, 2, 3, 4, 5);
        settings["militaryEducationPolicy"] = new JsonObject { ["mode"] = "mandatory" };

        var response = await Admin().PutAsJsonAsync(Url, settings);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
        Assert.Equal(60m, (decimal?)saved["attendanceThreshold"]);
        Assert.Equal("mandatory", (string?)saved["militaryEducationPolicy"]!["mode"]);
        Assert.NotNull((string?)saved["updatedAt"]);
        Assert.NotEqual(before, (uint)saved["version"]!);

        var publicSettings = (await Factory.CreateClient().GetFromJsonAsync<JsonObject>("/api/platform/settings"))!;
        Assert.Equal("جامعة القاهرة", (string?)publicSettings["universityNameAr"]);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, publicSettings["workingDays"]!.AsArray().Select(d => (int)d!).ToArray());
    }

    [Fact]
    public async Task Invalid_fields_return_400_naming_every_bad_field()
    {
        var settings = await GetAsAdmin();
        settings["attendanceThreshold"] = 120;
        settings["primaryColor"] = "blue";

        var response = await Admin().PutAsJsonAsync(Url, settings);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
        Assert.Equal("validation_failed", (string?)error["code"]);
        var fields = error["details"]!.AsArray().Select(d => (string?)d!["field"]).ToList();
        Assert.Contains("attendanceThreshold", fields);
        Assert.Contains("primaryColor", fields);
        Assert.Equal(75m, (decimal?)(await GetAsAdmin())["attendanceThreshold"]);   // nothing was saved
    }

    [Fact]
    public async Task A_business_rule_broken_in_the_domain_returns_400_with_the_field()
    {
        var settings = await GetAsAdmin();
        settings["timeZone"] = "Mars/Olympus_Mons";   // well-formed, but not a real IANA zone — only the domain knows

        var response = await Admin().PutAsJsonAsync(Url, settings);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
        Assert.Equal("invalid_value", (string?)error["code"]);
        Assert.Equal("timeZone", (string?)error["details"]![0]!["field"]);
    }

    [Fact]
    public async Task Saving_with_a_stale_version_is_a_409_conflict_not_a_silent_overwrite()
    {
        var loadedByAdminA = await GetAsAdmin();
        var loadedByAdminB = await GetAsAdmin();

        loadedByAdminA["universityName"] = "Saved by A";
        Assert.Equal(HttpStatusCode.OK, (await Admin().PutAsJsonAsync(Url, loadedByAdminA)).StatusCode);

        loadedByAdminB["universityName"] = "Saved by B";
        var response = await Admin().PutAsJsonAsync(Url, loadedByAdminB);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("conflict", (string?)(await response.Content.ReadFromJsonAsync<JsonObject>())!["code"]);
        Assert.Equal("Saved by A", (string?)(await GetAsAdmin())["universityName"]);
    }

    [Fact]
    public async Task A_malformed_body_returns_400_in_the_standard_envelope()
    {
        using var body = new StringContent("{ not json", Encoding.UTF8, "application/json");

        var response = await Admin().PutAsync(Url, body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("bad_request", (string?)(await response.Content.ReadFromJsonAsync<JsonObject>())!["code"]);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private async Task<JsonObject> GetAsAdmin() => (await Admin().GetFromJsonAsync<JsonObject>(Url))!;
}
