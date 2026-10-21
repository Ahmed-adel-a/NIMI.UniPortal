using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Audit.Application.Abstractions;
using NIMI.UniPortal.Modules.Audit.Contracts;
using NIMI.UniPortal.Modules.Audit.Domain;

namespace NIMI.UniPortal.Modules.Audit.Application.IntegrationEvents;

/// <summary>P10.2 — stores each audited action once (a replayed event finds its row and stops).</summary>
internal sealed class AuditableActionPerformedHandler(IAuditDbContext db) : IIntegrationEventHandler<AuditableActionPerformed>
{
    public async Task HandleAsync(AuditableActionPerformed integrationEvent, CancellationToken cancellationToken)
    {
        if (await db.ActivityLogs.AnyAsync(l => l.SourceEventId == integrationEvent.EventId, cancellationToken))
            return;

        db.ActivityLogs.Add(ActivityLog.Record(
            integrationEvent.EventId,
            integrationEvent.ActorId,
            integrationEvent.Action,
            integrationEvent.EntityType,
            integrationEvent.EntityId,
            integrationEvent.Summary,
            integrationEvent.OccurredAt));
        await db.SaveChangesAsync(cancellationToken);   // event handlers save once themselves
    }
}
