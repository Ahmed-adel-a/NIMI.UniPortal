using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Standings;

/// <summary>P4.7: a department's year groups, one row per level of its programme (graduated students excluded).</summary>
public sealed record GetCohortsQuery(Guid DepartmentId) : IQuery<IReadOnlyList<CohortLevelDto>>;
