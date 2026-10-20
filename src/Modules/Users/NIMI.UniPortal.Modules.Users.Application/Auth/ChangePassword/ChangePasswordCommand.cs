using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Users.Application.Auth.ChangePassword;

/// <summary>
/// The signed-in user replaces their password (US-X26). Returns a fresh token: the old one still says
/// <c>must_change_password = true</c>.
/// </summary>
public sealed record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : ICommand<TokenResponse>;
