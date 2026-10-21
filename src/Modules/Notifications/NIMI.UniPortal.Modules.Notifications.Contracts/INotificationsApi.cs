namespace NIMI.UniPortal.Modules.Notifications.Contracts;

/// <summary>What other modules (and the host's dashboards, P10) may read from Notifications.</summary>
public interface INotificationsApi
{
    /// <summary>How many of the user's notifications are still unread.</summary>
    Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken = default);
}
