using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Attendance.Application.Reports.AtRisk;

/// <summary>P6.7: students below the effective attendance threshold — a professor's own groups, or every group for Student Affairs.</summary>
public sealed record GetAtRiskQuery(
    Guid CallerId,
    bool IsAdmin,
    bool IsProfessor,
    Guid? TermId,
    Guid? GroupId,
    PageRequest Paging) : IQuery<PagedResult<AtRiskItemDto>>;
