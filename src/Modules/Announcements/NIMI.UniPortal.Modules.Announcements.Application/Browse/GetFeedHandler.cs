using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.Modules.Announcements.Application.Abstractions;
using NIMI.UniPortal.Modules.Announcements.Application.Common;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Announcements.Application.Browse;

/// <summary>
/// P7.5 — the student's groups this term (lifecycle) give the subjects; the feed holds those subjects' non-expired
/// announcements for all groups or for the student's own group, pinned first, then newest.
/// </summary>
internal sealed class GetFeedHandler(
    IAnnouncementsDbContext db,
    IAcademicLifecycle lifecycle,
    ISchedulingApi scheduling,
    AnnouncementPresenter presenter,
    TimeProvider clock) : IRequestHandler<GetFeedQuery, PagedResult<AnnouncementDto>>
{
    public async Task<PagedResult<AnnouncementDto>> Handle(GetFeedQuery query, CancellationToken cancellationToken)
    {
        if (!query.Caller.IsStudent)
            throw new ForbiddenException("The announcement feed is for students.");

        var term = await scheduling.GetCurrentTermAsync(cancellationToken);
        if (term is null)
            return new PagedResult<AnnouncementDto>([], query.Paging.Page, query.Paging.PageSize, 0);

        var groupIds = await lifecycle.GetGroupsForStudentAsync(query.Caller.UserId, term.Id, cancellationToken);
        var subjectIds = new HashSet<Guid>();
        foreach (var groupId in groupIds)
        {
            if (await scheduling.GetGroupAsync(groupId, cancellationToken) is { } group)
                subjectIds.Add(group.SubjectId);
        }

        var now = clock.GetUtcNow();
        var groups = groupIds.ToList();
        var subjects = subjectIds.ToList();
        var announcements = db.Announcements.AsNoTracking()
            .Where(a => a.TermId == term.Id && !a.IsDeleted && subjects.Contains(a.SubjectId))
            .Where(a => a.SubjectGroupId == null || groups.Contains(a.SubjectGroupId.Value))
            .Where(a => a.ExpiresAt == null || a.ExpiresAt > now);

        var total = await announcements.CountAsync(cancellationToken);
        var page = await announcements
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AnnouncementDto>(
            await presenter.PresentAllAsync(page, cancellationToken), query.Paging.Page, query.Paging.PageSize, total);
    }
}
