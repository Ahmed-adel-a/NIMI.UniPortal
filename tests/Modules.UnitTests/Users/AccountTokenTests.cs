// ACCEPTANCE TEST — written by the consultant for account extras (decision ACCOUNTX). Do not modify; report problems instead.
using NIMI.UniPortal.Modules.Users.Domain.Accounts;

namespace NIMI.UniPortal.Modules.UnitTests.Users;

/// <summary>Refresh-token rotation and single-use reset links.</summary>
public sealed class AccountTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Rotating_a_refresh_token_revokes_it_and_keeps_the_family()
    {
        var first = RefreshToken.Start(Guid.CreateVersion7(), "hash-1", Now);

        var second = first.Rotate("hash-2", Now.AddHours(1));

        Assert.False(first.IsActive(Now.AddHours(1)));
        Assert.Equal(second.Id, first.ReplacedById);
        Assert.Equal(first.FamilyId, second.FamilyId);
        Assert.True(second.IsActive(Now.AddHours(1)));
        Assert.Equal(Now.AddHours(1) + RefreshToken.Lifetime, second.ExpiresAt);
    }

    [Fact]
    public void A_refresh_token_expires_after_fourteen_days()
    {
        var token = RefreshToken.Start(Guid.CreateVersion7(), "hash", Now);

        Assert.True(token.IsActive(Now.AddDays(13)));
        Assert.False(token.IsActive(Now.AddDays(14)));
    }

    [Fact]
    public void A_reset_link_works_for_thirty_minutes_until_it_is_ended()
    {
        var token = PasswordResetToken.Issue(Guid.CreateVersion7(), "hash", Now);

        Assert.True(token.IsUsable(Now.AddMinutes(29)));
        Assert.False(token.IsUsable(Now.AddMinutes(30)));

        token.End(Now.AddMinutes(1));
        Assert.False(token.IsUsable(Now.AddMinutes(2)));
    }
}
