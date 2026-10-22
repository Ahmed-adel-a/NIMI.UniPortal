using NIMI.UniPortal.Modules.Forum.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Forum.Application.Threads;

/// <summary>Pin/unpin (<see cref="Pinned"/>) or lock/unlock (<see cref="Locked"/>) a thread; one of the two is set.</summary>
public sealed record ModerateThreadCommand(Guid ThreadId, bool? Pinned, bool? Locked, Viewer Caller) : ICommand<ThreadDto>
{
    public PageRequest Paging => new(1, null);
}
