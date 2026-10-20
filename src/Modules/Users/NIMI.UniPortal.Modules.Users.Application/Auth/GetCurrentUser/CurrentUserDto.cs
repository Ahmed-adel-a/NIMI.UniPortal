using NIMI.UniPortal.Modules.Users.Domain.Accounts;

namespace NIMI.UniPortal.Modules.Users.Application.Auth.GetCurrentUser;

/// <summary>GET /api/auth/me — who am I, and where do I belong.</summary>
public sealed record CurrentUserDto(
    Guid Id,
    string FullName,
    string Email,
    UserRole Role,
    string? StudentCode,
    AcademicTitle? AcademicTitle,
    Guid? DepartmentId,
    string? DepartmentCode,
    Guid? FacultyId,
    string? FacultyCode,
    int? EnrollmentYear,
    bool MustChangePassword,
    DateTimeOffset? LastLoginAt);
