using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Users.Application.Auth.Login;

/// <summary>
/// A command, not a query: it records failed attempts and the last login. It never throws on bad credentials —
/// it returns <c>null</c> so the attempt is still saved; the endpoint then answers 401.
/// <see cref="Email"/> takes an email or a national ID (decision NATIONALIDLOGIN). When one national ID has several
/// accounts whose password matches, <see cref="Role"/> picks one; without it the answer is 409 <c>choice_required</c>.
/// </summary>
public sealed record LoginCommand(string Email, string Password, string? Role = null) : ICommand<TokenResponse?>;
