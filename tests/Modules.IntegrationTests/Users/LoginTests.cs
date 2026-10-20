using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;

namespace NIMI.UniPortal.Modules.IntegrationTests.Users;

/// <summary>Login with real tokens, lockout, /me (US-X01 partial, US-S21 partial) — all through real JWT validation.</summary>
public sealed class LoginTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    private const string Email = "admin@cu.edu.eg";
    private const string Password = "Admin#2026";

    [Fact]
    public async Task A_user_logs_in_and_the_token_identifies_them()
    {
        var id = await Factory.CreateAdminAsync(Email, Password);

        var login = await Factory.LoginAsync(Email, Password);
        var me = (await Factory.CreateClientWithToken((string)login["accessToken"]!).GetFromJsonAsync<JsonObject>("/api/auth/me"))!;

        Assert.False((bool)login["mustChangePassword"]!);
        Assert.True(DateTimeOffset.Parse((string)login["expiresAt"]!) > DateTimeOffset.UtcNow.AddHours(7));   // 8-hour tokens
        Assert.Equal(id, (Guid)me["id"]!);
        Assert.Equal("admin", (string?)me["role"]);
        Assert.NotNull((string?)me["lastLoginAt"]);
    }

    [Fact]
    public async Task The_email_is_case_insensitive()
    {
        await Factory.CreateAdminAsync(Email, Password);

        var login = await Factory.LoginAsync("  ADMIN@CU.edu.EG ", Password);

        Assert.NotNull((string?)login["accessToken"]);
    }

    [Fact]
    public async Task Wrong_password_unknown_email_and_inactive_account_get_the_identical_401()
    {
        await Factory.CreateAdminAsync(Email, Password);
        await Factory.CreateAdminAsync("former@cu.edu.eg", Password, isActive: false);

        var wrongPassword = await TryLogin(Email, "nope-nope");
        var unknownEmail = await TryLogin("ghost@cu.edu.eg", Password);
        var inactive = await TryLogin("former@cu.edu.eg", Password);

        foreach (var response in new[] { wrongPassword, unknownEmail, inactive })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("invalid_credentials", (string?)(await response.Content.ReadFromJsonAsync<JsonObject>())!["code"]);
        }

        // No user enumeration: the bodies are byte-for-byte the same.
        Assert.Equal(await wrongPassword.Content.ReadAsStringAsync(), await unknownEmail.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Five_wrong_passwords_lock_the_account_for_15_minutes()
    {
        await Factory.CreateAdminAsync(Email, Password);

        for (var attempt = 1; attempt <= 5; attempt++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await TryLogin(Email, "wrong")).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await TryLogin(Email, Password)).StatusCode);   // locked: even the right password fails

        Factory.Clock.Advance(TimeSpan.FromMinutes(14));
        Assert.Equal(HttpStatusCode.Unauthorized, (await TryLogin(Email, Password)).StatusCode);   // still locked

        Factory.Clock.Advance(TimeSpan.FromMinutes(2));
        Assert.Equal(HttpStatusCode.OK, (await TryLogin(Email, Password)).StatusCode);             // lock expired
    }

    [Fact]
    public async Task A_successful_login_resets_the_failure_count()
    {
        await Factory.CreateAdminAsync(Email, Password);

        for (var attempt = 1; attempt <= 4; attempt++)
            await TryLogin(Email, "wrong");
        Assert.Equal(HttpStatusCode.OK, (await TryLogin(Email, Password)).StatusCode);
        for (var attempt = 1; attempt <= 4; attempt++)
            await TryLogin(Email, "wrong");

        Assert.Equal(HttpStatusCode.OK, (await TryLogin(Email, Password)).StatusCode);   // 4 + 4, never 5 in a row
    }

    [Fact]
    public async Task Our_token_opens_admin_only_endpoints()
    {
        await Factory.CreateAdminAsync(Email, Password);
        var token = (string)(await Factory.LoginAsync(Email, Password))["accessToken"]!;

        var response = await Factory.CreateClientWithToken(token).GetAsync("/api/admin/settings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task A_tampered_token_is_rejected()
    {
        await Factory.CreateAdminAsync(Email, Password);
        var token = (string)(await Factory.LoginAsync(Email, Password))["accessToken"]!;
        var tampered = token[..^4] + (token[^4..] == "AAAA" ? "BBBB" : "AAAA");   // break the signature

        var response = await Factory.CreateClientWithToken(tampered).GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_without_a_token_is_401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await Factory.CreateClient().GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task An_empty_login_is_a_400()
    {
        var response = await Factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email = "", password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private Task<HttpResponseMessage> TryLogin(string email, string password) =>
        Factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email, password });
}
