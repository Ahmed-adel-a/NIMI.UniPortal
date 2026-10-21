using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Catalog.ListTeachingAssignments;

public sealed record ListTeachingAssignmentsQuery(
    Guid? TermId,
    Guid? StaffId,
    Guid? SubjectId,
    PageRequest Paging) : IQuery<PagedResult<TeachingAssignmentDto>>;
