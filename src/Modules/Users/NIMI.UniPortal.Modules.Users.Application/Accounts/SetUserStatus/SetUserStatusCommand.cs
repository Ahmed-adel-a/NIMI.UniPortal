using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Users.Application.Accounts.SetUserStatus;

/// <summary>
/// US-A10: activate / deactivate an account (never deleted). A deactivated user can't log in.
/// <see cref="ActingAdminId"/> is the caller — admins can't deactivate themselves.
/// </summary>
public sealed record SetUserStatusCommand(Guid UserId, bool IsActive, Guid ActingAdminId) : ICommand<Unit>;
