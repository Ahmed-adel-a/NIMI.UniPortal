namespace NIMI.UniPortal.Modules.Users.Application.Auth;

/// <summary>
/// What login, a refresh and a password change return. When <see cref="MustChangePassword"/> is true the client must
/// send the user to the change-password screen; every other endpoint answers 403 <c>password_change_required</c>.
/// The access token is short-lived; exchange <see cref="RefreshToken"/> at <c>POST /api/auth/refresh</c> for a new
/// pair before <see cref="ExpiresAt"/> (decision ACCOUNTX). Each refresh token works once.
/// </summary>
public sealed record TokenResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    bool MustChangePassword,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
