using NIMI.UniPortal.Modules.Assignments.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Assignments.Application.Browse;

/// <summary>P8.2: assignments of one subject, or of every subject the caller studies / teaches this term; by due date.</summary>
public sealed record ListAssignmentsQuery(Guid? SubjectId, string? Status, Viewer Caller, PageRequest Paging)
    : IQuery<PagedResult<AssignmentDto>>;
