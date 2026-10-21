using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Abstractions;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.RegistrationWindows.CreateRegistrationWindow;

internal sealed class CreateRegistrationWindowHandler(
    ICreditHoursDbContext db, ISchedulingApi scheduling, TimeProvider clock)
    : IRequestHandler<CreateRegistrationWindowCommand, RegistrationWindowDto>
{
    public async Task<RegistrationWindowDto> Handle(
        CreateRegistrationWindowCommand command, CancellationToken cancellationToken)
    {
        var term = await scheduling.GetTermAsync(command.TermId, cancellationToken)
            ?? throw ValidationErrors.For(nameof(command.TermId), "Unknown term.");

        var now = clock.GetUtcNow();
        var window = RegistrationWindow.Create(
            term.Id, command.OpensAt, command.ClosesAt, command.MinCredits, command.MaxCredits, now);

        if (await db.RegistrationWindows.AnyAsync(w => w.TermId == window.TermId, cancellationToken))
            throw new ConflictException($"A registration window already exists for term '{term.Id}'.");

        db.RegistrationWindows.Add(window);
        return RegistrationWindowDto.From(window, now);
    }
}
