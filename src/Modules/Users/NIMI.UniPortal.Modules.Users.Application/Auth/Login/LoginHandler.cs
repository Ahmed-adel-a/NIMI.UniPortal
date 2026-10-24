using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Users.Application.Abstractions;
using NIMI.UniPortal.Modules.Users.Domain.Accounts;
using NIMI.UniPortal.SharedKernel;
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
        var now = clock.GetUtcNow();
        var candidates = await CandidatesAsync(command, cancellationToken);

        // Inactive and locked accounts fail exactly like a wrong password (same 401) — and don't count attempts.
        var usable = candidates.Where(u => u.IsActive && !u.IsLockedOut(now)).ToList();
        if (usable.Count == 0)
        {
            decoyHash ??= hasher.Hash(Guid.NewGuid().ToString("N"));
            hasher.Verify(decoyHash, command.Password);
            return null;
        }

        var matching = usable.Where(u => hasher.Verify(u.PasswordHash, command.Password)).ToList();
        if (matching.Count == 0)
        {
            foreach (var user in usable)
                user.RecordFailedLogin(now);   // saved by the UnitOfWork even though the login fails
            return null;
        }

        // Decision NATIONALIDLOGIN: the right password for several accounts — the person picks one. Asked only after
        // the password is proven, so it reveals nothing to someone who doesn't know it.
        if (matching.Count > 1)
        {
            throw new ChoiceRequiredException(
                "role",
                [.. matching.Select(u => UserRoles.Name(u.Role)).Order()],
                "This national ID has more than one account. Choose the role to sign in with.");
        }

        var chosen = matching[0];
        chosen.RecordSuccessfulLogin(now);
        return Sessions.Start(db, tokens, chosen, now);
    }

    /// <summary>The account for an email, or every account with a national ID (only the chosen role's, if given).</summary>
    private async Task<List<User>> CandidatesAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        var login = command.Email.Trim();
        var users = login.Contains('@')
            ? db.Users.Where(u => u.Email == login.ToLowerInvariant())
            : db.Users.Where(u => u.NationalId == login);
        if (UserRoles.TryParse(command.Role, out var role))
            users = users.Where(u => u.Role == role);
        return await users.ToListAsync(cancellationToken);
    }
}
