using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Users.Application.Abstractions;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Users.Application.Auth.Refresh;

internal sealed class RefreshSessionHandler(IUsersDbContext db, IAccessTokenIssuer tokens, TimeProvider clock)
    : IRequestHandler<RefreshSessionCommand, TokenResponse?>
{
    public async Task<TokenResponse?> Handle(RefreshSessionCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var hash = OpaqueToken.Hash(command.RefreshToken.Trim());
        var current = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (current is null || current.ExpiresAt <= now)
            return null;

        if (current.RevokedAt is not null)
        {
            // A rotated token used again: someone else holds a copy. End the whole sign-in (saved by the UnitOfWork).
            if (current.ReplacedById is not null)
                await Sessions.RevokeFamilyAsync(db, current.FamilyId, now, cancellationToken);
            return null;
        }

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == current.UserId, cancellationToken);
        if (user is null || !user.IsActive || user.IsLockedOut(now))
        {
            current.Revoke(now);
            return null;
        }

        return Sessions.Continue(db, tokens, user, current, now);
    }
}
