using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Notifications.Application.Notifications.MarkRead;

/// <summary>Marks one of the caller's notifications read; someone else's id is a 404 (rule R4).</summary>
public sealed record MarkNotificationReadCommand(Guid UserId, Guid NotificationId) : ICommand<Unit>;
