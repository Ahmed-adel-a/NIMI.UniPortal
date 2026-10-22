using NIMI.UniPortal.Modules.Forum.Application.Common;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Forum.Application.Threads;

internal sealed class EditThreadHandler(ForumAccess access, TimeProvider clock) : IRequestHandler<EditThreadCommand, ThreadDto>
{
    public async Task<ThreadDto> Handle(EditThreadCommand command, CancellationToken cancellationToken)
    {
        var thread = await access.VisibleThreadAsync(command.ThreadId, command.Caller, cancellationToken);
        var now = clock.GetUtcNow();
        if (!command.Caller.IsAdmin && !thread.CanAuthorEdit(command.Caller.UserId, now))
            throw new ForbiddenException("Only the author can edit a thread, within 15 minutes of posting it.");

        thread.Edit(command.Title ?? string.Empty, command.Body ?? string.Empty, now);
        return ThreadDto.WithoutPosts(thread, command.Paging);
    }
}
