using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Graduation;

/// <summary>P9.4: active students (university, faculty or department) with who can graduate and why not.</summary>
public sealed record GraduationEligibilityQuery(Guid? FacultyId, Guid? DepartmentId, PageRequest Paging)
    : IQuery<PagedResult<GraduationEligibilityDto>>;
