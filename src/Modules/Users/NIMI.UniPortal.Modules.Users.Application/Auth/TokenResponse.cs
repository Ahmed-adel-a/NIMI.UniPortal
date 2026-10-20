namespace NIMI.UniPortal.Modules.Users.Application.Auth;

/// <summary>
/// What login (and a password change) returns. When <see cref="MustChangePassword"/> is true the client must send
/// the user to the change-password screen — every other endpoint answers 403 <c>password_change_required</c>.
/// </summary>
public sealed record TokenResponse(string AccessToken, DateTimeOffset ExpiresAt, bool MustChangePassword);
