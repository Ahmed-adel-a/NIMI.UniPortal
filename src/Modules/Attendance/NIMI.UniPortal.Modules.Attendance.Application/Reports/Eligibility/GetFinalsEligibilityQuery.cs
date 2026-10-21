using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Attendance.Application.Reports.Eligibility;

/// <summary>P10.5: finals eligibility of every member of the visible groups that have closed sessions.</summary>
public sealed record GetFinalsEligibilityQuery(
    Guid CallerId,
    bool IsAdmin,
    bool IsProfessor,
    Guid? TermId,
    Guid? GroupId,
    Guid? FacultyId,
    bool BarredOnly,
    PageRequest Paging) : IQuery<PagedResult<FinalsEligibilityItemDto>>;
