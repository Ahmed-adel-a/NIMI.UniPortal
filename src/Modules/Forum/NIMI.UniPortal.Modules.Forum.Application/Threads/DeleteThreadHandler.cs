using NIMI.UniPortal.Modules.Forum.Application.Common;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Forum.Application.Threads;

internal sealed class DeleteThreadHandler(ForumAccess access, TimeProvider clock) : IRequestHandler<DeleteThreadCommand, Unit>
{
    public async Task<Unit> Handle(DeleteThreadCommand command, CancellationToken cancellationToken)
    {
        var thread = await access.VisibleThreadAsync(command.ThreadId, command.Caller, cancellationToken);
        var now = clock.GetUtcNow();
        if (!command.Caller.IsAdmin && !thread.CanAuthorEdit(command.Caller.UserId, now))
            throw new ForbiddenException("Only the author can remove a thread, within 15 minutes of posting it.");

        thread.Delete(now);
        return Unit.Value;
    }
}
