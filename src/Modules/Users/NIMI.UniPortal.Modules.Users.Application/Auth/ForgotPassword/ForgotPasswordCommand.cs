using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Users.Application.Auth.ForgotPassword;

/// <summary>
/// Decision ACCOUNTX: emails a single-use reset link (30 minutes) to an active account. The answer is the same
/// whether or not the address has an account, so it reveals nothing; at most three emails per account per hour.
/// </summary>
public sealed record ForgotPasswordCommand(string Email) : ICommand<Unit>;
