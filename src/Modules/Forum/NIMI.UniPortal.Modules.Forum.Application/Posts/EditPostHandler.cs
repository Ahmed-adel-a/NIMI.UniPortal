using NIMI.UniPortal.Modules.Forum.Application.Common;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Forum.Application.Posts;

internal sealed class EditPostHandler(VisiblePosts posts, TimeProvider clock) : IRequestHandler<EditPostCommand, PostDto>
{
    public async Task<PostDto> Handle(EditPostCommand command, CancellationToken cancellationToken)
    {
        var post = await posts.LoadAsync(command.PostId, command.Caller, cancellationToken);
        var now = clock.GetUtcNow();
        if (!command.Caller.IsAdmin && !post.CanAuthorEdit(command.Caller.UserId, now))
            throw new ForbiddenException("Only the author can edit a reply, within 15 minutes of posting it.");

        post.Edit(command.Body ?? string.Empty, now);
        return PostDto.From(post, command.Caller.IsAdmin);
    }
}
