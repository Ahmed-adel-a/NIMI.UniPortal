using NIMI.UniPortal.Modules.Users.Domain.Accounts;

namespace NIMI.UniPortal.Modules.UnitTests.Users;

/// <summary>Account rules: email normalization, lockout counting, password change / reset.</summary>
public sealed class UserLoginRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Emails_are_stored_trimmed_and_lower_case()
    {
        var user = User.CreateAdmin("Admin", "  Admin@CU.edu.EG ", "hash", false, Now);

        Assert.Equal("admin@cu.edu.eg", user.Email);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("a@")]
    [InlineData("")]
    [InlineData("two words@cu.edu.eg")]
    public void Invalid_emails_are_rejected(string email) =>
        Assert.Equal("email", Assert.Throws<ArgumentException>(() => User.CreateAdmin("Admin", email, "hash", false, Now)).ParamName);

    [Fact]
    public void The_fifth_consecutive_failure_locks_for_15_minutes_and_restarts_the_count()
    {
        var user = User.CreateAdmin("Admin", "a@cu.edu.eg", "hash", false, Now);

        for (var i = 1; i <= 4; i++)
            user.RecordFailedLogin(Now);
        Assert.False(user.IsLockedOut(Now));
        Assert.Equal(4, user.FailedLoginCount);

        user.RecordFailedLogin(Now);

        Assert.True(user.IsLockedOut(Now));
        Assert.True(user.IsLockedOut(Now.AddMinutes(14).AddSeconds(59)));
        Assert.False(user.IsLockedOut(Now.AddMinutes(15)));
        Assert.Equal(0, user.FailedLoginCount);
    }

    [Fact]
    public void A_successful_login_clears_failures_and_stamps_the_time()
    {
        var user = User.CreateAdmin("Admin", "a@cu.edu.eg", "hash", false, Now);
        user.RecordFailedLogin(Now);

        user.RecordSuccessfulLogin(Now.AddMinutes(1));

        Assert.Equal(0, user.FailedLoginCount);
        Assert.Equal(Now.AddMinutes(1), user.LastLoginAt);
    }

    [Fact]
    public void Changing_the_password_ends_the_forced_change()
    {
        var user = User.CreateAdmin("Admin", "a@cu.edu.eg", "temp-hash", mustChangePassword: true, Now);

        user.ChangePassword("new-hash", Now);

        Assert.False(user.MustChangePassword);
        Assert.Equal("new-hash", user.PasswordHash);
    }

    [Fact]
    public void An_admin_reset_forces_a_change_and_unlocks()
    {
        var user = User.CreateAdmin("Admin", "a@cu.edu.eg", "hash", false, Now);
        for (var i = 1; i <= 5; i++)
            user.RecordFailedLogin(Now);

        user.ResetPassword("temp-hash", Now);

        Assert.True(user.MustChangePassword);
        Assert.False(user.IsLockedOut(Now));
        Assert.Equal("temp-hash", user.PasswordHash);
    }
}
