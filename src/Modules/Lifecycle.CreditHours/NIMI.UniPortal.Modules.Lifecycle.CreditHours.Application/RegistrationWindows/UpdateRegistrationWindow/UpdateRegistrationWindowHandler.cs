using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Abstractions;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.RegistrationWindows.UpdateRegistrationWindow;

internal sealed class UpdateRegistrationWindowHandler(ICreditHoursDbContext db, TimeProvider clock)
    : IRequestHandler<UpdateRegistrationWindowCommand, RegistrationWindowDto>
{
    public async Task<RegistrationWindowDto> Handle(
        UpdateRegistrationWindowCommand command, CancellationToken cancellationToken)
    {
        var window = await db.RegistrationWindows
            .FirstOrDefaultAsync(w => w.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException($"No registration window with id '{command.Id}'.");

        var now = clock.GetUtcNow();
        window.Update(command.OpensAt, command.ClosesAt, command.MinCredits, command.MaxCredits, now);

        return RegistrationWindowDto.From(window, now);
    }
}
