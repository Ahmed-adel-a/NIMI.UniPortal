using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Abstractions;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;
using NIMI.UniPortal.Modules.Payments.Contracts;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.IntegrationEvents;

/// <summary>
/// P5.5 — confirms the student's pending military-education registration once the military-education fee is fully
/// paid (R6/MILEDU). PaymentCompleted is raised by Payments for office and online payments alike; only
/// <c>military_education</c> fully-paid events reach the confirmation. Replays are no-ops (no pending row).
/// </summary>
internal sealed class PaymentCompletedHandler(ICreditHoursDbContext db, TimeProvider clock)
    : IIntegrationEventHandler<PaymentCompleted>
{
    private const string MilitaryEducation = "military_education";

    public async Task HandleAsync(
        PaymentCompleted integrationEvent, CancellationToken cancellationToken)
    {
        if (!integrationEvent.FeeFullyPaid ||
            !string.Equals(integrationEvent.Category, MilitaryEducation, StringComparison.OrdinalIgnoreCase))
            return;

        // pending_payment is created only by the military flow (normal registration rejects military subjects),
        // so every pending row of this student belongs to that flow. There is no term on PaymentCompleted; when a
        // student has more than one pending row, paying the fee confirms each one whose fee was requested.
        var pending = await db.CourseRegistrations
            .Where(r => r.StudentId == integrationEvent.StudentId &&
                        r.Status == RegistrationStatus.PendingPayment)
            .OrderBy(r => r.RegisteredAt)
            .ThenBy(r => r.Id)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
            return;

        var now = clock.GetUtcNow();
        foreach (var registration in pending)
            registration.ConfirmPayment(now);

        await db.SaveChangesAsync(cancellationToken);   // event handlers save once themselves
    }
}
