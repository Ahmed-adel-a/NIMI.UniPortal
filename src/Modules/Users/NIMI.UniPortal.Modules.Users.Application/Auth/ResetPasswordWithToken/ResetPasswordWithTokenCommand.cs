using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Users.Application.Auth.ResetPasswordWithToken;

/// <summary>
/// Decision ACCOUNTX: the emailed token sets a new password of the user's choice, clears a lockout and the forced
/// change, and ends every session and every other reset link. 400 on <c>token</c> when it is unknown, used or expired.
/// </summary>
public sealed record ResetPasswordWithTokenCommand(string Token, string NewPassword) : ICommand<Unit>;
