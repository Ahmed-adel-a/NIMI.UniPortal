using NIMI.UniPortal.Modules.Notifications.Application.Notifications.UnreadCount;
using NIMI.UniPortal.Modules.Notifications.Contracts;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Notifications.Application;

/// <summary>The Notifications contract, answered by the module's own queries (one rule, one place).</summary>
internal sealed class NotificationsApi(ISender sender) : INotificationsApi
{
    public async Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken = default) =>
        (await sender.Send(new GetUnreadCountQuery(userId), cancellationToken)).Count;
}
