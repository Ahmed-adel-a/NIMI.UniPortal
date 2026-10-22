namespace NIMI.UniPortal.Modules.Users.Domain.Accounts;

/// <summary>
/// A "forgot password" link (decision ACCOUNTX). Only the SHA-256 hash of the emailed token is stored; it is valid
/// for <see cref="Lifetime"/>, once, and a newer request or a completed reset ends every older one.
/// </summary>
public sealed class PasswordResetToken
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    /// <summary>At most this many reset emails per account per hour; further requests are silently ignored.</summary>
    public const int MaxRequestsPerHour = 3;

    private PasswordResetToken()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>When it was used, or ended by a newer request or a completed reset.</summary>
    public DateTimeOffset? EndedAt { get; private set; }

    public static PasswordResetToken Issue(Guid userId, string tokenHash, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(),
        UserId = userId,
        TokenHash = tokenHash,
        CreatedAt = now,
        ExpiresAt = now + Lifetime,
    };

    public bool IsUsable(DateTimeOffset now) => EndedAt is null && ExpiresAt > now;

    public void End(DateTimeOffset now) => EndedAt ??= now;
}
