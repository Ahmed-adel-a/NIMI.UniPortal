using NIMI.UniPortal.Modules.Forum.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Forum.Application.Threads;

/// <summary>A category's threads: pinned first, then by latest activity.</summary>
public sealed record ListThreadsQuery(Guid CategoryId, Viewer Caller, PageRequest Paging) : IQuery<PagedResult<ThreadSummaryDto>>;
