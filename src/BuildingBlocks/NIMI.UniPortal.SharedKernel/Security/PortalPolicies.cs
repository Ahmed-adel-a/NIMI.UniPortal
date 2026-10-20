namespace NIMI.UniPortal.SharedKernel.Security;

/// <summary>Authorization policy names, defined once by the host and used by every module's endpoints.</summary>
public static class PortalPolicies
{
    /// <summary>Student Affairs (role <c>admin</c>).</summary>
    public const string Admin = "Admin";
}

/// <summary>The three roles of the portal (user-stories.md §1).</summary>
public static class PortalRoles
{
    public const string Student = "student";
    public const string Professor = "professor";
    public const string Admin = "admin";
}
