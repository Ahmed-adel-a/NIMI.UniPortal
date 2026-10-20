using System.Security.Claims;

namespace NIMI.UniPortal.SharedKernel.Security;

/// <summary>Claim names inside our access tokens (kept short, JWT-style — inbound claim mapping is off).</summary>
public static class PortalClaims
{
    public const string UserId = "sub";
    public const string Name = "name";
    public const string Email = "email";
    public const string Role = "role";
    public const string DepartmentId = "department_id";

    /// <summary>"true" until the user replaces their temporary password (decision AUTH).</summary>
    public const string MustChangePassword = "must_change_password";

    /// <summary>The signed-in user's id (works for real tokens and for the test scheme).</summary>
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirst(UserId)?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var id) ? id : throw new InvalidOperationException("The caller has no user id claim.");
    }
}
