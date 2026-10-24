namespace NIMI.UniPortal.Modules.Users.Domain.Accounts;

/// <summary>
/// The text name of each <see cref="UserRole"/>: the same in the database, in tokens, in JSON and in other modules
/// (<c>student</c>, <c>professor</c>, <c>department_head</c>, <c>admin</c>).
/// </summary>
public static class UserRoles
{
    public static string Name(UserRole role) => role switch
    {
        UserRole.Student => "student",
        UserRole.Professor => "professor",
        UserRole.DepartmentHead => "department_head",
        UserRole.Admin => "admin",
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown role."),
    };

    /// <summary>Reads a role name, case-insensitively; also accepts the enum spelling (<c>DepartmentHead</c>).</summary>
    public static bool TryParse(string? text, out UserRole role)
    {
        foreach (var candidate in Enum.GetValues<UserRole>())
        {
            if (string.Equals(text?.Trim(), Name(candidate), StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text?.Trim(), candidate.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                role = candidate;
                return true;
            }
        }

        role = default;
        return false;
    }

    public static UserRole Parse(string text) =>
        TryParse(text, out var role) ? role : throw new ArgumentException($"Unknown role '{text}'.", nameof(text));
}
