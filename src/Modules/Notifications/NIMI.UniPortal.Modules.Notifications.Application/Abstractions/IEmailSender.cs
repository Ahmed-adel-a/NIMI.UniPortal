namespace NIMI.UniPortal.Modules.Notifications.Application.Abstractions;

/// <summary>
/// The email provider port (decision ACCOUNTX; the real provider is still open, EMAILPROVIDER). Development and tests
/// use the <c>outbox</c> provider, which only logs: the outbox row is the delivery.
/// </summary>
public interface IEmailSender
{
    string Provider { get; }

    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken);
}
