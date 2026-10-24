using System.Security.Claims;

namespace NIMI.UniPortal.SharedKernel.Security;

/// <summary>The roles of the portal (user-stories.md §1; department head: decision ROLES2).</summary>
public static class PortalRoles
{
    public const string Student = "student";
    public const string Professor = "professor";
    public const string DepartmentHead = "department_head";
    public const string Admin = "admin";

    /// <summary>
    /// Roles that may be assigned to teach and then get the teaching-staff screens for their subjects. Teaching
    /// assistants use the professor role; a department head may also teach (decision ROLES2).
    /// </summary>
    public static readonly IReadOnlyList<string> TeachingStaff = [Professor, DepartmentHead];

    public static bool CanTeach(string? role) => role is not null && TeachingStaff.Contains(role);

    /// <summary>True when the caller holds a teaching-staff role (see <see cref="TeachingStaff"/>).</summary>
    public static bool IsTeachingStaff(this ClaimsPrincipal principal) => TeachingStaff.Any(principal.IsInRole);
}
