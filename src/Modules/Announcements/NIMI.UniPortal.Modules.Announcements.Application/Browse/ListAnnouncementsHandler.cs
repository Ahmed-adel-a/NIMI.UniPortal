using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Announcements.Application.Abstractions;
using NIMI.UniPortal.Modules.Announcements.Application.Common;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Announcements.Application.Browse;

/// <summary>
/// P7.5 — teachers of the subject and Student Affairs see everything, expired items included; a registered student
/// sees the non-expired subject-wide items and their own group's (R2, R6); anyone else is refused.
/// </summary>
internal sealed class ListAnnouncementsHandler(
    IAnnouncementsDbContext db,
    CourseAccess access,
    ISchedulingApi scheduling,
    AnnouncementPresenter presenter,
    TimeProvider clock) : IRequestHandler<ListAnnouncementsQuery, PagedResult<AnnouncementDto>>
{
    public async Task<PagedResult<AnnouncementDto>> Handle(ListAnnouncementsQuery query, CancellationToken cancellationToken)
    {
        if (query.SubjectId is not { } subjectId)
            throw ValidationErrors.For("subjectId", "Choose a subject.");

        var subject = await scheduling.GetSubjectAsync(subjectId, cancellationToken)
            ?? throw new NotFoundException("Subject not found.");
        var term = await access.ActiveTermAsync(cancellationToken);

        var announcements = db.Announcements.AsNoTracking()
            .Where(a => a.SubjectId == subject.Id && a.TermId == term.Id && !a.IsDeleted);

        var seesEverything = query.Caller.IsAdmin ||
                             (query.Caller.IsProfessor &&
                              await access.TeachesSubjectAsync(query.Caller.UserId, subject.Id, term.Id, cancellationToken));
        if (!seesEverything)
        {
            var groups = query.Caller.IsStudent
                ? await access.StudentGroupsAsync(query.Caller.UserId, subject.Id, term.Id, cancellationToken)
                : [];
            if (groups.Count == 0)
                throw new ForbiddenException("You are not registered in or teaching this subject.");

            var now = clock.GetUtcNow();
            announcements = announcements.Where(a =>
                (a.SubjectGroupId == null || groups.Contains(a.SubjectGroupId.Value)) &&
                (a.ExpiresAt == null || a.ExpiresAt > now));
        }

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
