namespace NIMI.UniPortal.Modules.Users.Api.Endpoints;

public sealed record ResetPasswordRequest(string Token, string NewPassword);
