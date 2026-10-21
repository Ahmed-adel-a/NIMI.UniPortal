using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Catalog.ListGroups;

public sealed record ListGroupsQuery(Guid SubjectId, Guid? TermId, PageRequest Paging)
    : IQuery<PagedResult<GroupDto>>;
