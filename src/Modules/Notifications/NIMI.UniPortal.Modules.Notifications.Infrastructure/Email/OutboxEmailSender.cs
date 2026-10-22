using Microsoft.Extensions.Logging;
using NIMI.UniPortal.Modules.Notifications.Application.Abstractions;

namespace NIMI.UniPortal.Modules.Notifications.Infrastructure.Email;

/// <summary>
/// The development provider (<c>Email:Provider=outbox</c>, refused in Production): nothing leaves the server. The
/// outbox row is the delivery, readable at <c>GET /api/admin/dev/emails</c>.
/// </summary>
internal sealed class OutboxEmailSender(ILogger<OutboxEmailSender> logger) : IEmailSender
{
    public const string Name = "outbox";

    public string Provider => Name;

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        logger.LogInformation("Email kept in the outbox (not sent): to {To}, subject {Subject}.", to, subject);
        return Task.CompletedTask;
    }
}
