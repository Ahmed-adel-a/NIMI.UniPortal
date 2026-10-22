namespace NIMI.UniPortal.Modules.Notifications.Domain;

/// <summary>
/// One email in the outbox (notifications.outbound_emails): what was asked, through which provider, and when it went
/// out. With the development provider (<c>outbox</c>) nothing leaves the server: the row is the delivery.
/// </summary>
public sealed class OutboundEmail
{
    private OutboundEmail()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>The <c>EmailRequested.EventId</c>: one row per event.</summary>
    public Guid SourceEventId { get; private set; }

    public string To { get; private set; } = null!;

    public string Subject { get; private set; } = null!;

    public string Body { get; private set; } = null!;

    public string Provider { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? SentAt { get; private set; }

    public static OutboundEmail Create(Guid sourceEventId, string to, string subject, string body, string provider, DateTimeOffset now) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            SourceEventId = sourceEventId,
            To = to.Trim(),
            Subject = subject.Trim(),
            Body = body,
            Provider = provider,
            CreatedAt = now,
        };

    public void MarkSent(DateTimeOffset now) => SentAt = now;
}
