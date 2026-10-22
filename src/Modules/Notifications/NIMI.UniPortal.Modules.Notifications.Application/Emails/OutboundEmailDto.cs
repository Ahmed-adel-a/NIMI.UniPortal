namespace NIMI.UniPortal.Modules.Notifications.Application.Emails;

/// <summary>An email in the outbox, as the development outbox endpoint shows it.</summary>
public sealed record OutboundEmailDto(
    Guid Id, string To, string Subject, string Body, string Provider, DateTimeOffset CreatedAt, DateTimeOffset? SentAt);
