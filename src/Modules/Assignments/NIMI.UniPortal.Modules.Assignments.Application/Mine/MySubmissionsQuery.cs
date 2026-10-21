using NIMI.UniPortal.Modules.Assignments.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Assignments.Application.Mine;

/// <summary>P8.6: the signed-in student's submissions, newest first.</summary>
public sealed record MySubmissionsQuery(string? Status, Viewer Caller, PageRequest Paging) : IQuery<PagedResult<MySubmissionDto>>;
