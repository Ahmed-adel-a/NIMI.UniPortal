using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;

namespace NIMI.UniPortal.Modules.IntegrationTests.Users;

/// <summary>Change password + forced change on first login (US-X26, decision AUTH).</summary>
public sealed class ChangePasswordTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    private const string Email = "new.admin@cu.edu.eg";
    private const string TemporaryPassword = "Temp1234x";
    private const string ChosenPassword = "MyOwn#Pass9";

    [Fact]
    public async Task First_login_with_a_temporary_password_is_held_until_the_password_is_changed()
    {
        await Factory.CreateAdminAsync(Email, TemporaryPassword, mustChangePassword: true);

        var firstLogin = await Factory.LoginAsync(Email, TemporaryPassword);
        Assert.True((bool)firstLogin["mustChangePassword"]!);
        var held = Factory.CreateClientWithToken((string)firstLogin["accessToken"]!);

        // Everything is blocked with a clear code … except "who am I" and "change my password".
        var blocked = await held.GetAsync("/api/admin/settings");
        Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);
        Assert.Equal("password_change_required", (string?)(await blocked.Content.ReadFromJsonAsync<JsonObject>())!["code"]);
        Assert.Equal(HttpStatusCode.Forbidden, (await held.GetAsync("/api/faculties")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await held.GetAsync("/api/auth/me")).StatusCode);

        var change = await held.PostAsJsonAsync("/api/auth/change-password", new { currentPassword = TemporaryPassword, newPassword = ChosenPassword });
        Assert.Equal(HttpStatusCode.OK, change.StatusCode);
        var fresh = (await change.Content.ReadFromJsonAsync<JsonObject>())!;
        Assert.False((bool)fresh["mustChangePassword"]!);

        // The fresh token works everywhere; the temporary password is gone.
        Assert.Equal(HttpStatusCode.OK, (await Factory.CreateClientWithToken((string)fresh["accessToken"]!).GetAsync("/api/admin/settings")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await TryLogin(Email, TemporaryPassword)).StatusCode);
        Assert.False((bool)(await Factory.LoginAsync(Email, ChosenPassword))["mustChangePassword"]!);
    }

    [Fact]
    public async Task A_wrong_current_password_is_a_400_on_that_field()
    {
        var client = await SignedIn();

        var response = await client.PostAsJsonAsync("/api/auth/change-password", new { currentPassword = "not-it-1", newPassword = ChosenPassword });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("currentPassword", (string?)(await response.Content.ReadFromJsonAsync<JsonObject>())!["details"]![0]!["field"]);
    }

    [Theory]
    [InlineData("short1")]          // < 8
    [InlineData("lettersonly")]     // no digit
    [InlineData("12345678")]        // no letter
    [InlineData(TemporaryPassword)] // same as current
    public async Task Weak_or_unchanged_new_passwords_are_a_400_on_newPassword(string newPassword)
    {
        var client = await SignedIn();

        var response = await client.PostAsJsonAsync("/api/auth/change-password", new { currentPassword = TemporaryPassword, newPassword });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var fields = (await response.Content.ReadFromJsonAsync<JsonObject>())!["details"]!.AsArray().Select(d => (string?)d!["field"]);
        Assert.Contains("newPassword", fields);
    }

    [Fact]
    public async Task Anonymous_callers_cannot_change_passwords()
    {
        var response = await Factory.CreateClient().PostAsJsonAsync("/api/auth/change-password",
            new { currentPassword = TemporaryPassword, newPassword = ChosenPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<HttpClient> SignedIn()
    {
        await Factory.CreateAdminAsync(Email, TemporaryPassword, mustChangePassword: true);
        return Factory.CreateClientWithToken((string)(await Factory.LoginAsync(Email, TemporaryPassword))["accessToken"]!);
    }

    private Task<HttpResponseMessage> TryLogin(string email, string password) =>
        Factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email, password });
}
