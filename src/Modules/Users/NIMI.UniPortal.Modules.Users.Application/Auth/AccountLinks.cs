namespace NIMI.UniPortal.Modules.Users.Application.Auth;

/// <summary>Where emailed links point: the frontend's base address (<c>App:FrontendUrl</c>).</summary>
public sealed class AccountLinks
{
    public string FrontendUrl { get; set; } = "http://localhost:5173";

    public string ResetPassword(string token) => $"{FrontendUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(token)}";
}
