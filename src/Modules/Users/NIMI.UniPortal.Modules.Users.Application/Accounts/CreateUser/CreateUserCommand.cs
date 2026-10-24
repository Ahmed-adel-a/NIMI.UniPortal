using NIMI.UniPortal.Modules.Users.Domain.Accounts;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Users.Application.Accounts.CreateUser;

/// <summary>
/// US-A01 / US-A10: Student Affairs creates an account.
/// Student → department + enrollment year (student code generated). Professor → department + academic title.
/// Department head (decision ROLES2) → department, optional academic title, one active per department.
/// Admin → no department. Password optional: if absent a temporary one is generated and returned once.
/// Every new account must change its password at first login.
/// </summary>
public sealed record CreateUserCommand(
    UserRole Role,
    string FullName,
    string Email,
    string? Password,
    Guid? DepartmentId,
    int? EnrollmentYear,
    AcademicTitle? AcademicTitle,
    string? NationalId,
    DateOnly? DateOfBirth,
    Gender? Gender,
    string? Phone) : ICommand<CreatedUserDto>;
