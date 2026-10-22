using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Users.Application.Abstractions;
using NIMI.UniPortal.Modules.Users.Domain.Accounts;

namespace NIMI.UniPortal.Modules.Users.Application.Auth;

/// <summary>
/// Decision ACCOUNTX: a session is an access token plus a rotating refresh token. Tracked changes are saved by the
/// caller's UnitOfWork.
/// </summary>
internal static class Sessions
{
    /// <summary>A new sign-in: a fresh access token and the first refresh token of a new family.</summary>
    public static TokenResponse Start(IUsersDbContext db, IAccessTokenIssuer tokens, User user, DateTimeOffset now)
    {
        var (value, hash) = OpaqueToken.New();
        var refresh = RefreshToken.Start(user.Id, hash, now);
        db.RefreshTokens.Add(refresh);
        return Response(tokens, user, value, refresh);
    }

    /// <summary>Rotation: the presented token is replaced by a new one of the same family.</summary>
    public static TokenResponse Continue(IUsersDbContext db, IAccessTokenIssuer tokens, User user, RefreshToken current, DateTimeOffset now)
    {
        var (value, hash) = OpaqueToken.New();
        var next = current.Rotate(hash, now);
        db.RefreshTokens.Add(next);
        return Response(tokens, user, value, next);
    }

    /// <summary>Ends every session of the user (password changed or reset, account deactivated).</summary>
    public static async Task RevokeAllAsync(IUsersDbContext db, Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var active = await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
            .ToListAsync(cancellationToken);
        foreach (var token in active)
            token.Revoke(now);
    }

    /// <summary>Ends every token of one sign-in (logout, or a stolen token presented again).</summary>
    public static async Task RevokeFamilyAsync(IUsersDbContext db, Guid familyId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var family = await db.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var token in family)
            token.Revoke(now);
    }

    private static TokenResponse Response(IAccessTokenIssuer tokens, User user, string refreshValue, RefreshToken refresh)
    {
        var access = tokens.Issue(user);
        return new TokenResponse(access.Value, access.ExpiresAt, user.MustChangePassword, refreshValue, refresh.ExpiresAt);
    }
}
