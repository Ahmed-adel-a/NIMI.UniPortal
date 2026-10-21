using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Notifications.Application.Notifications.MarkAllRead;

/// <summary>Marks every unread notification of the caller read; 204 even when nothing was left (rule R4).</summary>
public sealed record MarkAllNotificationsReadCommand(Guid UserId) : ICommand<Unit>;
