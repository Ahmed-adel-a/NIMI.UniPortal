using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Abstractions;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Promotion;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Promotion;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Standing;
using NIMI.UniPortal.Modules.Payments.Contracts;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.IntegrationEvents;

/// <summary>
/// Decision SEMPROMO: a payment may lift a held year-end decision. When nothing is outstanding any more, the held
/// year is decided by the rule; otherwise nothing changes (replays are no-ops).
/// </summary>
internal sealed class PaymentCompletedHandler(ISemesterDbContext db, PromotionService promotions, IEventPublisher events)
    : IIntegrationEventHandler<PaymentCompleted>
{
    public async Task HandleAsync(PaymentCompleted integrationEvent, CancellationToken cancellationToken)
    {
        var standing = await db.Standings.SingleOrDefaultAsync(
            s => s.StudentId == integrationEvent.StudentId && s.Status == StandingStatus.OnHold, cancellationToken);
        if (standing is null)
            return;

        var heldYear = await db.YearPromotions
            .Where(p => p.StudentId == standing.StudentId && p.Result == PromotionResult.Held)
            .OrderByDescending(p => p.DecidedAt)
            .Select(p => (int?)p.AcademicYear)
            .FirstOrDefaultAsync(cancellationToken);
        if (heldYear is null)
            return;

        var decision = await promotions.DecideAgainAsync(standing, heldYear.Value, cancellationToken);
        if (decision is null)
            return;

        await db.SaveChangesAsync(cancellationToken);
        await events.PublishAsync(PromotionNotices.For(decision), cancellationToken);
    }
}
