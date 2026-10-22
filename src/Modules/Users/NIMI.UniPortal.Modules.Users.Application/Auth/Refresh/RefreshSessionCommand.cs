using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Users.Application.Auth.Refresh;

/// <summary>
/// Decision ACCOUNTX: exchanges a refresh token for a new access token and a new refresh token. Null (401) when the
/// token is unknown, expired or revoked, or the account can no longer sign in; a token presented a second time
/// revokes its whole sign-in.
/// </summary>
public sealed record RefreshSessionCommand(string RefreshToken) : ICommand<TokenResponse?>;
