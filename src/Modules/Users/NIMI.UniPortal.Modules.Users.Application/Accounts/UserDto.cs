using NIMI.UniPortal.Modules.Users.Domain.Accounts;

namespace NIMI.UniPortal.Modules.Users.Application.Accounts;

/// <summary>An account as Student Affairs sees it (never includes the password hash).</summary>
public sealed record UserDto(
    Guid Id,
    string FullName,
    string Email,
    UserRole Role,
    string? StudentCode,
    AcademicTitle? AcademicTitle,
    Guid? DepartmentId,
    string? DepartmentCode,
    string? FacultyCode,
    int? EnrollmentYear,
    bool IsActive,
    bool MustChangePassword,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt);
