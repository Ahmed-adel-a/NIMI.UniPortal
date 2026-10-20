namespace NIMI.UniPortal.Modules.Users.Api.Endpoints;

/// <summary>POST /api/auth/change-password body (the user comes from the token, never from the body).</summary>
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
