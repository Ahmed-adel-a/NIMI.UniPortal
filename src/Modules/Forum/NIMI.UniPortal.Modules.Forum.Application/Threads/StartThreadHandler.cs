using NIMI.UniPortal.Modules.Forum.Application.Abstractions;
using NIMI.UniPortal.Modules.Forum.Application.Common;
using NIMI.UniPortal.Modules.Forum.Domain;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Forum.Application.Threads;

internal sealed class StartThreadHandler(
    IForumDbContext db, ForumAccess access, AuthorDirectory authors, ISchedulingApi scheduling, TimeProvider clock)
    : IRequestHandler<StartThreadCommand, ThreadDto>
{
    public async Task<ThreadDto> Handle(StartThreadCommand command, CancellationToken cancellationToken)
    {
        var category = await access.VisibleCategoryAsync(command.CategoryId, command.Caller, cancellationToken);
        if (command.SubjectId is { } subjectId && await scheduling.GetSubjectAsync(subjectId, cancellationToken) is null)
            throw ValidationErrors.For("subjectId", "Unknown subject.");

        var now = clock.GetUtcNow();
        await PostingRateLimit.EnsureAllowedAsync(db, command.Caller.UserId, now, cancellationToken);

        var author = await authors.ForAsync(command.Caller, cancellationToken);
        var thread = ForumThread.Start(category.Id, command.SubjectId, author, command.Title ?? string.Empty, command.Body ?? string.Empty, now);
        db.Threads.Add(thread);
        return ThreadDto.WithoutPosts(thread, new PageRequest(1, null));
    }
}
