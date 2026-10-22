using NIMI.UniPortal.EventBus;

namespace NIMI.UniPortal.Modules.Notifications.Contracts;

/// <summary>
/// Decision ACCOUNTX: a producer asks for one email to one address (e.g. a password-reset link). Notifications keeps
/// it in the outbox (idempotent on <see cref="EventId"/>) and hands it to the configured email provider.
/// The body is plain text; producers write it in English and Arabic.
/// </summary>
public sealed record EmailRequested(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string To,
    string Subject,
    string Body) : IIntegrationEvent;
