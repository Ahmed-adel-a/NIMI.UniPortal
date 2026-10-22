// ACCEPTANCE TEST — written by the consultant for account extras (decision ACCOUNTX). Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Users.AccountSessionSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Users;

/// <summary>
/// Sessions: login returns a refresh token that renews the session (14 days), rotates on every use, ends the whole
/// sign-in when a used one is presented again, and ends on logout, a password change or deactivation.
/// </summary>
public sealed class RefreshTokenTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task A_refresh_token_renews_the_session_with_a_new_pair()
    {
        var (_, email) = await StudentAsync(Factory, Factory.CreateClientAs("admin"), "ahmed@cu.edu.eg");
        var session = await SignedInAsync(Factory, email);
        Assert.False(string.IsNullOrWhiteSpace((string?)session["refreshToken"]));
        Assert.NotNull(session["refreshTokenExpiresAt"]);

        var response = await RefreshAsync(Factory, (string)session["refreshToken"]!);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var renewed = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
        Assert.NotEqual((string?)session["refreshToken"], (string?)renewed["refreshToken"]);
        var me = await Factory.CreateClientWithToken((string)renewed["accessToken"]!).GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    [Fact]
    public async Task A_refresh_token_works_once_and_a_replay_ends_the_sign_in()
    {
        var (_, email) = await StudentAsync(Factory, Factory.CreateClientAs("admin"), "ahmed@cu.edu.eg");
        var first = (string)(await SignedInAsync(Factory, email))["refreshToken"]!;
        var second = (string)(await (await RefreshAsync(Factory, first)).Content.ReadFromJsonAsync<JsonObject>())!["refreshToken"]!;

        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(Factory, first)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(Factory, second)).StatusCode);
    }

    [Fact]
    public async Task Logout_ends_the_sign_in_and_never_fails()
    {
        var (_, email) = await StudentAsync(Factory, Factory.CreateClientAs("admin"), "ahmed@cu.edu.eg");
        var token = (string)(await SignedInAsync(Factory, email))["refreshToken"]!;

        var logout = await Factory.CreateClient().PostAsJsonAsync("/api/auth/logout", new { refreshToken = token });
        var unknown = await Factory.CreateClient().PostAsJsonAsync("/api/auth/logout", new { refreshToken = "not-a-token" });

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(Factory, token)).StatusCode);
    }

    [Fact]
    public async Task Changing_the_password_ends_the_other_sessions_and_starts_a_new_one()
    {
        var (_, email) = await StudentAsync(Factory, Factory.CreateClientAs("admin"), "ahmed@cu.edu.eg");
        var here = await SignedInAsync(Factory, email);
        var elsewhere = await SignedInAsync(Factory, email);

        var change = await Factory.CreateClientWithToken((string)here["accessToken"]!)
            .PostAsJsonAsync("/api/auth/change-password", new { currentPassword = Password, newPassword = "Brand!New2027" });

        Assert.Equal(HttpStatusCode.OK, change.StatusCode);
        var fresh = (await change.Content.ReadFromJsonAsync<JsonObject>())!;
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(Factory, (string)elsewhere["refreshToken"]!)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await RefreshAsync(Factory, (string)fresh["refreshToken"]!)).StatusCode);
    }

    [Fact]
    public async Task An_expired_refresh_token_or_a_deactivated_account_cannot_refresh()
    {
        var admin = Factory.CreateClientAs("admin");
        var (id, email) = await StudentAsync(Factory, admin, "ahmed@cu.edu.eg");
        var expiring = (string)(await SignedInAsync(Factory, email))["refreshToken"]!;
        var active = (string)(await SignedInAsync(Factory, email))["refreshToken"]!;

        Assert.Equal(HttpStatusCode.NoContent,
            (await admin.PatchAsJsonAsync($"/api/admin/users/{id}/status", new { isActive = false })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(Factory, active)).StatusCode);

        Factory.Clock.Advance(TimeSpan.FromDays(15));
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(Factory, expiring)).StatusCode);
    }
}
