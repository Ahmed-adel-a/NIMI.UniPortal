using NIMI.UniPortal.Modules.Users.Domain.Accounts;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Users.Application.Accounts.ListUsers;

/// <summary>
/// US-A10: find accounts. Every filter is optional; <see cref="Search"/> matches name, email or student code
/// (case-insensitive, contains). Newest accounts first.
/// </summary>
public sealed record ListUsersQuery(
    UserRole? Role,
    Guid? FacultyId,
    Guid? DepartmentId,
    bool? IsActive,
    string? Search,
    PageRequest Paging) : IQuery<PagedResult<UserDto>>;
