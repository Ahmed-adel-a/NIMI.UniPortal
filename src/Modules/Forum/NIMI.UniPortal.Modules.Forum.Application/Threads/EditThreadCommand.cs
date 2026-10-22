using NIMI.UniPortal.Modules.Forum.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Forum.Application.Threads;

/// <summary>The author (15-minute window) or Student Affairs (any time) edits a thread's title and body.</summary>
public sealed record EditThreadCommand(Guid ThreadId, string? Title, string? Body, Viewer Caller) : ICommand<ThreadDto>
{
    public PageRequest Paging => new(1, null);
}
