using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Notifications.Application.Abstractions;
using NIMI.UniPortal.Modules.Notifications.Contracts;
using NIMI.UniPortal.Modules.Notifications.Domain;

namespace NIMI.UniPortal.Modules.Notifications.Application.IntegrationEvents;

/// <summary>
/// Decision ACCOUNTX: keeps the email in the outbox (once per event), hands it to the provider and records when it
/// went out. A provider failure leaves no row, and the EventBus logs it (the producer is never failed).
/// </summary>
internal sealed class EmailRequestedHandler(INotificationsDbContext db, IEmailSender sender, TimeProvider clock)
    : IIntegrationEventHandler<EmailRequested>
{
    public async Task HandleAsync(EmailRequested integrationEvent, CancellationToken cancellationToken)
    {
        if (await db.OutboundEmails.AnyAsync(e => e.SourceEventId == integrationEvent.EventId, cancellationToken))
            return;

        var email = OutboundEmail.Create(
            integrationEvent.EventId, integrationEvent.To, integrationEvent.Subject, integrationEvent.Body, sender.Provider,
            clock.GetUtcNow());
        await sender.SendAsync(email.To, email.Subject, email.Body, cancellationToken);
        email.MarkSent(clock.GetUtcNow());

        db.OutboundEmails.Add(email);
        await db.SaveChangesAsync(cancellationToken);   // event handlers save once themselves
    }
}
