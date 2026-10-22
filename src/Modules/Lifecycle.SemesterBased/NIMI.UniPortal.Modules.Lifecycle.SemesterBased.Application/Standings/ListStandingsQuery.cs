using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Standing;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Standings;

/// <summary>P4.7: standings filtered by department, level and status, paged.</summary>
public sealed record ListStandingsQuery(Guid? DepartmentId, int? Level, StandingStatus? Status, PageRequest Paging)
    : IQuery<PagedResult<StandingDto>>;
