using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Notifications.Contracts;
using NIMI.UniPortal.Modules.Payments.Application.Abstractions;
using NIMI.UniPortal.Modules.Payments.Domain.Fees;

namespace NIMI.UniPortal.Modules.Payments.Application.IntegrationEvents;

/// <summary>
/// P5.8 — answers <see cref="MilitaryEducationRequested"/> (produced by the lifecycle variant in P5.5) by creating
/// the student's fee for the event term's active military-education item, else the annual one, and notifying the
/// student. Idempotent on (fee item, student); saves once, then publishes the notification.
/// </summary>
internal sealed class MilitaryEducationRequestedHandler(
    IPaymentsDbContext db,
    IEventPublisher events,
    TimeProvider clock,
    ILogger<MilitaryEducationRequestedHandler> logger) : IIntegrationEventHandler<MilitaryEducationRequested>
{
    public async Task HandleAsync(
        MilitaryEducationRequested integrationEvent, CancellationToken cancellationToken)
    {
        var items = await db.FeeItems
            .AsNoTracking()
            .Where(f => f.Category == FeeCategory.MilitaryEducation && f.IsActive)
            .OrderBy(f => f.DueOn)
            .ThenBy(f => f.Id)
            .ToListAsync(cancellationToken);

        var item = items.FirstOrDefault(f => f.TermId == integrationEvent.TermId)
                   ?? items.FirstOrDefault(f => f.TermId is null);

        if (item is null)
        {
            logger.LogWarning(
                "MilitaryEducationRequested {EventId}: no active military-education fee item for term {TermId} or annual.",
                integrationEvent.EventId,
                integrationEvent.TermId);
            return;
        }

        var exists = await db.StudentFees
            .AsNoTracking()
            .AnyAsync(
                f => f.FeeItemId == item.Id && f.StudentId == integrationEvent.StudentId,
                cancellationToken);

        if (exists)
            return;

        var now = clock.GetUtcNow();
        db.StudentFees.Add(StudentFee.Create(item, integrationEvent.StudentId, now));
        await db.SaveChangesAsync(cancellationToken);   // event handlers save once themselves

        await events.PublishAsync(
            new NotificationRequested(
                Guid.CreateVersion7(),
                now,
                [integrationEvent.StudentId],
                "payment",
                "Military education fee",
                "رسوم التربية العسكرية",
                $"A military-education fee of {item.Amount:0.##} has been added to your account.",
                $"تم إضافة رسوم التربية العسكرية بقيمة {item.Amount:0.##} إلى حسابك.",
                "/payments"),
            cancellationToken);
    }
}
