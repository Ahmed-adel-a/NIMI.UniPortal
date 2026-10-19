using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;

/// <summary>
/// Test-only authentication: the caller's role comes from the X-Test-Role header.
/// No header → anonymous (so 401 paths stay testable). Stands in for real JWTs until P1.1 ships login.
/// </summary>
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string RoleHeader = "X-Test-Role";

    /// <summary>Stable user id per role, so tests can assert "who did it".</summary>
    public static Guid UserIdFor(string role) => role switch
    {
        "admin" => Guid.Parse("0199a000-0000-7000-8000-000000000001"),
        "professor" => Guid.Parse("0199a000-0000-7000-8000-000000000002"),
        "student" => Guid.Parse("0199a000-0000-7000-8000-000000000003"),
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Roles are student / professor / admin."),
    };

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(RoleHeader, out var values) || string.IsNullOrWhiteSpace(values))
            return Task.FromResult(AuthenticateResult.NoResult());

        var role = values.ToString().Trim().ToLowerInvariant();
        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, UserIdFor(role).ToString()),
            new(ClaimTypes.Name, $"test-{role}"),
            new(ClaimTypes.Role, role),
        ];

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
