using NIMI.UniPortal.Modules.Forum.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Forum.Application.Threads;

/// <summary>
/// Open a thread with one page of replies. A command, not a query: when the author opens it, that is recorded so the
/// next reply may notify them again (one unread notice per thread).
/// </summary>
public sealed record ViewThreadCommand(Guid ThreadId, Viewer Caller, PageRequest Paging) : ICommand<ThreadDto>;
