using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Users.Application.Accounts.ResetPassword;

/// <summary>
/// US-A10: Student Affairs resets a forgotten password. A new temporary password is returned once; the user must
/// change it at the next login, and any lockout is cleared.
/// </summary>
public sealed record ResetPasswordCommand(Guid UserId) : ICommand<TemporaryPasswordDto>;
