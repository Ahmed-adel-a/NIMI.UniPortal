using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Catalog.ListMyClasses;

/// <summary>Decision FRONTEND: the groups the caller teaches in the active term (whole-subject assignments cover every group).</summary>
public sealed record ListMyClassesQuery(Guid StaffId, bool IsTeachingStaff) : IQuery<IReadOnlyList<TeachingClassDto>>;
