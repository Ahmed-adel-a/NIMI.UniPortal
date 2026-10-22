using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Rosters.GetGroupRoster;

/// <summary>
/// P4.4 — one group's roster. <paramref name="IsAdmin"/> skips the teaching check (admin route);
/// otherwise the caller must be a professor assigned to the subject/group (rule R5).
/// </summary>
public sealed record GetGroupRosterQuery(
    Guid GroupId,
    Guid StaffId,
    bool IsProfessor,
    bool IsAdmin,
    PageRequest Paging) : IQuery<PagedResult<RosterStudentDto>>;
