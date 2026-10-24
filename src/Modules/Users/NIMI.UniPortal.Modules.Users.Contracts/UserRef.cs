namespace NIMI.UniPortal.Modules.Users.Contracts;

/// <summary>
/// An account as other modules see it. <see cref="Role"/> is "student" / "professor" / "department_head" / "admin" (plain text, so
/// consumers don't depend on the Users domain).
/// </summary>
public sealed record UserRef(
    Guid Id,
    string FullName,
    string Role,
    Guid? DepartmentId,
    string? StudentCode,
    string? AcademicTitle,
    bool IsActive);
