using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Forum.Application.Abstractions;
using NIMI.UniPortal.Modules.Forum.Application.Common;
using NIMI.UniPortal.Modules.Forum.Domain;
using NIMI.UniPortal.Modules.Notifications.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Forum.Application.Posts;

/// <summary>
/// A locked thread refuses replies (409); posting is rate limited (429). The thread's author is notified (type
/// <c>forum</c>) unless it is their own reply or they still have an unread notice for this thread.
/// </summary>
internal sealed class ReplyHandler(
    IForumDbContext db, ForumAccess access, AuthorDirectory authors, IEventCollector events, TimeProvider clock)
    : IRequestHandler<ReplyCommand, PostDto>
{
    public async Task<PostDto> Handle(ReplyCommand command, CancellationToken cancellationToken)
    {
        var thread = await access.VisibleThreadAsync(command.ThreadId, command.Caller, cancellationToken);
        if (thread.IsLocked)
            throw new ConflictException("This thread is locked.");
        if (thread.IsDeleted)
            throw new ConflictException("This thread was removed.");

        var now = clock.GetUtcNow();
        await PostingRateLimit.EnsureAllowedAsync(db, command.Caller.UserId, now, cancellationToken);

        var author = await authors.ForAsync(command.Caller, cancellationToken);
        var post = ForumPost.Write(thread.Id, author, command.Body ?? string.Empty, now);
        db.Posts.Add(post);

        if (thread.AcceptReply(author, now))
        {
            var title = thread.Title.Length <= 150 ? thread.Title : thread.Title[..147] + "...";
            events.Add(new NotificationRequested(
                Guid.CreateVersion7(),
                now,
                [thread.Author.Id],
                "forum",
                $"New reply: {title}",
                $"رد جديد: {title}",
                $"{author.Name} replied to your thread.",
                $"رد {author.Name} على موضوعك.",
                $"/forum/threads/{thread.Id}"));
        }

        return PostDto.From(post, command.Caller.IsAdmin);
    }
}
