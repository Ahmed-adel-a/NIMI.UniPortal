using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Users.Application.Abstractions;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Users.Application.Auth.Login;

internal sealed class LoginHandler(IUsersDbContext db, IPasswordHasher hasher, IAccessTokenIssuer tokens, TimeProvider clock)
    : IRequestHandler<LoginCommand, TokenResponse?>
{
    /// <summary>
    /// A real hash of a random secret, verified against when the email is unknown — so "no such account" costs the
    /// same hashing time as a wrong password and response timing can't reveal which emails exist.
    /// </summary>
    private static string? decoyHash;

    public async Task<TokenResponse?> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var email = command.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email, cancellationToken);
        var now = clock.GetUtcNow();

        if (user is null)
        {
            decoyHash ??= hasher.Hash(Guid.NewGuid().ToString("N"));
            hasher.Verify(decoyHash, command.Password);
            return null;
        }

        // Inactive and locked accounts fail exactly like a wrong password (same 401) — and don't count attempts.
        if (!user.IsActive || user.IsLockedOut(now))
            return null;

        if (!hasher.Verify(user.PasswordHash, command.Password))
        {
            user.RecordFailedLogin(now);   // saved by the UnitOfWork even though the login fails
            return null;
        }

        user.RecordSuccessfulLogin(now);
        var token = tokens.Issue(user);
        return new TokenResponse(token.Value, token.ExpiresAt, user.MustChangePassword);
    }
}
