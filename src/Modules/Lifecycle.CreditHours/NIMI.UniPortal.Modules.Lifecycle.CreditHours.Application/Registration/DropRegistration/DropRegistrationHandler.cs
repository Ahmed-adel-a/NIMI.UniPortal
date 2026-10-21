using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Abstractions;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Registration.DropRegistration;

internal sealed class DropRegistrationHandler(
    ICreditHoursDbContext db, ISeatCounter seats, TimeProvider clock)
    : IRequestHandler<DropRegistrationCommand, Unit>
{
    private const string Field = "registrationId";

    public async Task<Unit> Handle(DropRegistrationCommand command, CancellationToken cancellationToken)
    {
        if (!command.IsStudent)
            throw new ForbiddenException("Only students can drop a course.");

        var registration = await db.CourseRegistrations
            .FirstOrDefaultAsync(
                r => r.Id == command.RegistrationId && r.StudentId == command.StudentId,
                cancellationToken)
            ?? throw new NotFoundException($"No registration with id '{command.RegistrationId}'.");

        if (registration.Status != RegistrationStatus.Registered)
            throw ValidationErrors.For(Field, "Only a registered course can be dropped.");

        var now = clock.GetUtcNow();
        var window = await db.RegistrationWindows.AsNoTracking()
            .FirstOrDefaultAsync(w => w.TermId == registration.TermId, cancellationToken);
        if (window is null || !window.IsOpen(now))
            throw ValidationErrors.For(Field, "Registration is closed.");

        await seats.BeginAsync(cancellationToken);
        try
        {
            // Rule R8: the seat comes back in the same transaction as the status change.
            await seats.ReleaseAsync(registration.SubjectGroupId, cancellationToken);

            registration.Drop(now);
            return Unit.Value;
        }
        catch
        {
            await seats.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
