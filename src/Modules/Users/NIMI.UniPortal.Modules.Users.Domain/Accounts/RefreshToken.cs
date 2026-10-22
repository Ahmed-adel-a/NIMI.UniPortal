namespace NIMI.UniPortal.Modules.Users.Domain.Accounts;

/// <summary>
/// A long-lived session credential (decision ACCOUNTX): exchanged for a new access token and rotated on every use.
/// All tokens of one sign-in share a <see cref="FamilyId"/>; presenting an already-rotated token means it leaked,
/// so the whole family is revoked. Only the SHA-256 hash is stored.
/// </summary>
public sealed class RefreshToken
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(14);

    private RefreshToken()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid FamilyId { get; private set; }

    public string TokenHash { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>Set when the token was rotated (as opposed to revoked by logout or a password change).</summary>
    public Guid? ReplacedById { get; private set; }

    /// <summary>The first token of a sign-in starts a new family.</summary>
    public static RefreshToken Start(Guid userId, string tokenHash, DateTimeOffset now) =>
        Create(userId, Guid.CreateVersion7(), tokenHash, now);

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;

    /// <summary>Rotates this token: it is revoked and replaced by a new one of the same family.</summary>
    public RefreshToken Rotate(string newTokenHash, DateTimeOffset now)
    {
        var next = Create(UserId, FamilyId, newTokenHash, now);
        RevokedAt = now;
        ReplacedById = next.Id;
        return next;
    }

    public void Revoke(DateTimeOffset now) => RevokedAt ??= now;

    private static RefreshToken Create(Guid userId, Guid familyId, string tokenHash, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(),
        UserId = userId,
        FamilyId = familyId,
        TokenHash = tokenHash,
        CreatedAt = now,
        ExpiresAt = now + Lifetime,
    };
}
