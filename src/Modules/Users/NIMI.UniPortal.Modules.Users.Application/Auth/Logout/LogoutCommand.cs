using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Users.Application.Auth.Logout;

/// <summary>
/// Ends the sign-in the refresh token belongs to (every token of its family). Unknown tokens are ignored, so logout
/// always succeeds; the access token simply runs out.
/// </summary>
public sealed record LogoutCommand(string RefreshToken) : ICommand<Unit>;
