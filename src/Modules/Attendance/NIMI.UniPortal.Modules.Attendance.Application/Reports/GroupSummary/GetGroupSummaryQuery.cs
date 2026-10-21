using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Attendance.Application.Reports.GroupSummary;

/// <summary>P6.6: attendance percentage per student of one group — for its teaching staff and Student Affairs.</summary>
public sealed record GetGroupSummaryQuery(Guid GroupId, Guid CallerId, bool IsAdmin, PageRequest Paging)
    : IQuery<PagedResult<GroupSummaryItemDto>>;
