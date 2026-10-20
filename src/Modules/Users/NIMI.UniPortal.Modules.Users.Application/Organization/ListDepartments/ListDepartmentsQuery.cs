using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Users.Application.Organization.ListDepartments;

/// <summary>Departments ordered by code, optionally of one faculty; inactive ones only on request.</summary>
public sealed record ListDepartmentsQuery(Guid? FacultyId, bool IncludeInactive, PageRequest Paging) : IQuery<PagedResult<DepartmentDto>>;
