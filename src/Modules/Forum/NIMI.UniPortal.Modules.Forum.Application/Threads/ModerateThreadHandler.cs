using NIMI.UniPortal.Modules.Forum.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Forum.Application.Threads;

internal sealed class ModerateThreadHandler(ForumAccess access, TimeProvider clock) : IRequestHandler<ModerateThreadCommand, ThreadDto>
{
    public async Task<ThreadDto> Handle(ModerateThreadCommand command, CancellationToken cancellationToken)
    {
        var thread = await access.VisibleThreadAsync(command.ThreadId, command.Caller, cancellationToken);
        await access.EnsureCanModerateAsync(thread, command.Caller, cancellationToken);

        var now = clock.GetUtcNow();
        if (command.Pinned is { } pinned)
            thread.SetPinned(pinned, now);
        if (command.Locked is { } locked)
            thread.SetLocked(locked, now);

        return ThreadDto.WithoutPosts(thread, command.Paging);
    }
}
