using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Platform.Application.Abstractions;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Platform.Application.Settings.DeleteFacultySettings;

internal sealed class DeleteFacultySettingsHandler(IPlatformDbContext db, IUsersApi users)
    : IRequestHandler<DeleteFacultySettingsCommand, Unit>
{
    public async Task<Unit> Handle(DeleteFacultySettingsCommand command, CancellationToken cancellationToken)
    {
        // R1: an unknown faculty is a 404 even though deleting twice is fine.
        _ = await users.GetFacultyAsync(command.FacultyId, cancellationToken)
            ?? throw new NotFoundException("Faculty not found.");

        var settings = await db.FacultySettings
            .SingleOrDefaultAsync(f => f.FacultyId == command.FacultyId, cancellationToken);

        if (settings is not null)
            db.FacultySettings.Remove(settings);

        return Unit.Value;
    }
}
