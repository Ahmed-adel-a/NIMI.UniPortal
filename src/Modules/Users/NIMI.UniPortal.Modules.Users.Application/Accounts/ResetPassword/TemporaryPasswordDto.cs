namespace NIMI.UniPortal.Modules.Users.Application.Accounts.ResetPassword;

/// <summary>Shown once to the admin; only its hash is stored.</summary>
public sealed record TemporaryPasswordDto(string TemporaryPassword);
