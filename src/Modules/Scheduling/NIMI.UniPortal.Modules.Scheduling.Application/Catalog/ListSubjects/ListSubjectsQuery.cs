using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Catalog.ListSubjects;

public sealed record ListSubjectsQuery(
    Guid? DepartmentId,
    string? Search,
    bool IncludeInactive,
    PageRequest Paging) : IQuery<PagedResult<SubjectDto>>;
