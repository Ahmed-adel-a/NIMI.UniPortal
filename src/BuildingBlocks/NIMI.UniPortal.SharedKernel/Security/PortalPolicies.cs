namespace NIMI.UniPortal.SharedKernel.Security;

/// <summary>Authorization policy names, defined once by the host and used by every module's endpoints.</summary>
public static class PortalPolicies
{
    /// <summary>Student Affairs (role <c>admin</c>).</summary>
    public const string Admin = "Admin";

    /// <summary>
    /// Signed in, even while a temporary password still has to be replaced. Only for /api/auth/me and
    /// /api/auth/change-password; every other protected endpoint requires the password to have been changed.
    /// </summary>
    public const string PendingPasswordChange = "PendingPasswordChange";
}
