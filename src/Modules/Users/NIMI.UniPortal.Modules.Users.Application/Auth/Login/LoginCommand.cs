using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Users.Application.Auth.Login;

/// <summary>
/// A command, not a query: it records failed attempts and the last login. It never throws on bad credentials —
/// it returns <c>null</c> so the attempt is still saved; the endpoint then answers 401.
/// </summary>
public sealed record LoginCommand(string Email, string Password) : ICommand<TokenResponse?>;
