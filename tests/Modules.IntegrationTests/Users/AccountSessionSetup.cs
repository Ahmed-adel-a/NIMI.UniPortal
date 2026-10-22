// ACCEPTANCE TEST — written by the consultant for account extras (decision ACCOUNTX). Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;

namespace NIMI.UniPortal.Modules.IntegrationTests.Users;

/// <summary>Sessions and password-reset helpers through the real API.</summary>
internal static partial class AccountSessionSetup
{
    /// <summary>The password <see cref="AccountSetup"/> gives every account it creates.</summary>
    public const string Password = "Portal!Pass2026";

    public static async Task<(Guid Id, string Email)> StudentAsync(IPortalHost host, HttpClient admin, string email)
    {
        var faculty = await OrganizationSetup.CreateFacultyAsync(admin, "ENG");
        var department = await OrganizationSetup.CreateDepartmentAsync(admin, faculty, "CS");
        var (id, _) = await AccountSetup.CreateStudentWithTokenAsync(host, admin, department, email);
        return (id, email);
    }

    public static async Task<HttpResponseMessage> LoginAsync(IPortalHost host, string email, string password) =>
        await host.CreateClient().PostAsJsonAsync("/api/auth/login", new { email, password });

    public static async Task<JsonObject> SignedInAsync(IPortalHost host, string email, string password = Password)
    {
        var response = await LoginAsync(host, email, password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonObject>())!;
    }

    public static async Task<HttpResponseMessage> RefreshAsync(IPortalHost host, string refreshToken) =>
        await host.CreateClient().PostAsJsonAsync("/api/auth/refresh", new { refreshToken });

    public static async Task<HttpResponseMessage> ForgotAsync(IPortalHost host, string email) =>
        await host.CreateClient().PostAsJsonAsync("/api/auth/forgot-password", new { email });

    public static async Task<HttpResponseMessage> ResetAsync(IPortalHost host, string token, string newPassword) =>
        await host.CreateClient().PostAsJsonAsync("/api/auth/reset-password", new { token, newPassword });

    public static async Task<JsonArray> OutboxAsync(IPortalHost host, string to) =>
        (await host.CreateClientAs("admin").GetFromJsonAsync<JsonObject>($"/api/admin/dev/emails?to={Uri.EscapeDataString(to)}"))!
            ["items"]!.AsArray();

    /// <summary>The token of the newest reset link sent to the address.</summary>
    public static async Task<string> LatestResetTokenAsync(IPortalHost host, string to)
    {
        var body = (string)(await OutboxAsync(host, to)).First()!["body"]!;
        return Uri.UnescapeDataString(ResetLink().Match(body).Groups[1].Value);
    }

    [GeneratedRegex(@"reset-password\?token=([A-Za-z0-9_\-%]+)")]
    private static partial Regex ResetLink();
}
