using NIMI.UniPortal.Modules.Announcements.Application.Abstractions;
using NIMI.UniPortal.Modules.Announcements.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Announcements.Application.Manage;

internal sealed class DeleteAnnouncementHandler(IAnnouncementsDbContext db, CourseAccess access, TimeProvider clock)
    : IRequestHandler<DeleteAnnouncementCommand, Unit>
{
    public async Task<Unit> Handle(DeleteAnnouncementCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var announcement = await AnnouncementOwnership.LoadManageableAsync(
            db, access, command.AnnouncementId, command.Caller, now, cancellationToken);

        announcement.Delete(now);
        return Unit.Value;
    }
}
