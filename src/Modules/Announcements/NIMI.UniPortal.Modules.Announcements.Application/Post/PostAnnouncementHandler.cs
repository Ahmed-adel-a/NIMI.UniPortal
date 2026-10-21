using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Announcements.Application.Abstractions;
using NIMI.UniPortal.Modules.Announcements.Application.Common;
using NIMI.UniPortal.Modules.Announcements.Domain;
using NIMI.UniPortal.Modules.Notifications.Contracts;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Announcements.Application.Post;

/// <summary>
/// P7.4 — only staff teaching the subject this term post (D1, R1); a target group must belong to the subject. An
/// urgent announcement notifies its students after the commit (R5).
/// </summary>
internal sealed class PostAnnouncementHandler(
    IAnnouncementsDbContext db,
    CourseAccess access,
    ISchedulingApi scheduling,
    AnnouncementPresenter presenter,
    IEventCollector events,
    TimeProvider clock) : IRequestHandler<PostAnnouncementCommand, AnnouncementDto>
{
    public async Task<AnnouncementDto> Handle(PostAnnouncementCommand command, CancellationToken cancellationToken)
    {
        if (!command.Caller.IsProfessor)
            throw new ForbiddenException("Only teaching staff post announcements.");

        var term = await access.ActiveTermAsync(cancellationToken);
        var subject = await scheduling.GetSubjectAsync(command.SubjectId, cancellationToken)
            ?? throw ValidationErrors.For("subjectId", "Unknown subject.");

        if (!await access.TeachesSubjectAsync(command.Caller.UserId, subject.Id, term.Id, cancellationToken))
            throw new ForbiddenException("You do not teach this subject this term.");

        if (command.SubjectGroupId is { } groupId)
        {
            var group = await scheduling.GetGroupAsync(groupId, cancellationToken);
            if (group is null || group.SubjectId != subject.Id || group.TermId != term.Id)
                throw ValidationErrors.For("subjectGroupId", "The group is not a group of this subject in the active term.");
        }

        var now = clock.GetUtcNow();
        var announcement = Announcement.Create(
            subject.Id,
            term.Id,
            command.SubjectGroupId,
            command.Caller.UserId,
            command.Title,
            command.Body,
            command.Priority,
            command.IsPinned,
            command.ExpiresAt,
            now);
        db.Announcements.Add(announcement);

        if (announcement.Priority == AnnouncementPriority.Urgent)
        {
            var audience = await access.AudienceAsync(subject.Id, term.Id, command.SubjectGroupId, cancellationToken);
            if (audience.Count > 0)
            {
                events.Add(new NotificationRequested(
                    Guid.CreateVersion7(),
                    now,
                    audience,
                    "announcement",
                    Clip($"{subject.Code}: {announcement.Title}", 200),
                    Clip($"{subject.Code}: {announcement.Title}", 200),
                    Clip(announcement.Body, 2000),
                    Clip(announcement.Body, 2000),
                    $"/announcements/{announcement.Id}"));
            }
        }

        return await presenter.PresentAsync(announcement, cancellationToken);
    }

    /// <summary>Notification titles hold 200 characters and messages 2000; the full text stays on the announcement.</summary>
    private static string Clip(string text, int max) => text.Length <= max ? text : text[..(max - 3)] + "...";
}
