using NIMI.UniPortal.Modules.Assignments.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Assignments.Application.Review;

/// <summary>P8.4: an assignment's submissions for its teaching staff, in hand-in order.</summary>
public sealed record ListSubmissionsQuery(Guid AssignmentId, string? Status, Viewer Caller, PageRequest Paging)
    : IQuery<PagedResult<SubmissionDto>>;
