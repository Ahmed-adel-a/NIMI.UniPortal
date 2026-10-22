using NIMI.UniPortal.Modules.Forum.Application.Common;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Forum.Application.Posts;

internal sealed class DeletePostHandler(VisiblePosts posts, TimeProvider clock) : IRequestHandler<DeletePostCommand, Unit>
{
    public async Task<Unit> Handle(DeletePostCommand command, CancellationToken cancellationToken)
    {
        var post = await posts.LoadAsync(command.PostId, command.Caller, cancellationToken);
        var now = clock.GetUtcNow();
        if (!command.Caller.IsAdmin && !post.CanAuthorEdit(command.Caller.UserId, now))
            throw new ForbiddenException("Only the author can remove a reply, within 15 minutes of posting it.");

        post.Delete(now);
        return Unit.Value;
    }
}
