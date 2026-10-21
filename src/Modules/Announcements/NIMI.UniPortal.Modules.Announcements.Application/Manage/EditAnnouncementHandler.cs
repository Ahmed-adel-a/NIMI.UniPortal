using NIMI.UniPortal.Modules.Announcements.Application.Abstractions;
using NIMI.UniPortal.Modules.Announcements.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Announcements.Application.Manage;

internal sealed class EditAnnouncementHandler(
    IAnnouncementsDbContext db, CourseAccess access, AnnouncementPresenter presenter, TimeProvider clock)
    : IRequestHandler<EditAnnouncementCommand, AnnouncementDto>
{
    public async Task<AnnouncementDto> Handle(EditAnnouncementCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var announcement = await AnnouncementOwnership.LoadManageableAsync(
            db, access, command.AnnouncementId, command.Caller, now, cancellationToken);

        announcement.Edit(command.Title, command.Body, command.Priority, command.IsPinned, command.ExpiresAt, now);
        return await presenter.PresentAsync(announcement, cancellationToken);
    }
}
