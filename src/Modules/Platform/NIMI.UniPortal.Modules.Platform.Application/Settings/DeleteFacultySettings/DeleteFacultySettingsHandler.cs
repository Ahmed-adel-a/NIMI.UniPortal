using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Audit.Contracts;
using NIMI.UniPortal.Modules.Platform.Application.Abstractions;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Platform.Application.Settings.DeleteFacultySettings;

internal sealed class DeleteFacultySettingsHandler(
    IPlatformDbContext db, IUsersApi users, IEventCollector events, ICurrentUser currentUser, TimeProvider clock)
    : IRequestHandler<DeleteFacultySettingsCommand, Unit>
{
    public async Task<Unit> Handle(DeleteFacultySettingsCommand command, CancellationToken cancellationToken)
    {
        // R1: an unknown faculty is a 404 even though deleting twice is fine.
        var faculty = await users.GetFacultyAsync(command.FacultyId, cancellationToken)
            ?? throw new NotFoundException("Faculty not found.");

        var settings = await db.FacultySettings
            .SingleOrDefaultAsync(f => f.FacultyId == command.FacultyId, cancellationToken);

        if (settings is null)
            return Unit.Value;   // already gone: nothing changed, nothing to audit

        db.FacultySettings.Remove(settings);
        events.Add(new AuditableActionPerformed(
            Guid.CreateVersion7(),
            clock.GetUtcNow(),
            currentUser.UserId,
            AuditActions.FacultySettingsRemoved,
            AuditEntities.Faculty,
            faculty.Id,
            $"Academic-rule overrides of faculty {faculty.Code} removed; university defaults apply."));

        return Unit.Value;
    }
}
