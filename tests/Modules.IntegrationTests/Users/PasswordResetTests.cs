// ACCEPTANCE TEST — written by the consultant for account extras (decision ACCOUNTX). Do not modify; report problems instead.
using System.Net;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Users.AccountSessionSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Users;

/// <summary>
/// "Forgot password": a single-use link emailed to an active account, valid 30 minutes, newest only, at most three
/// an hour. The request never reveals whether an account exists.
/// </summary>
public sealed class PasswordResetTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    private const string NewPassword = "Brand!New2027";

    [Fact]
    public async Task Known_and_unknown_emails_get_the_same_answer_but_only_accounts_get_a_link()
    {
        var (_, email) = await StudentAsync(Factory, Factory.CreateClientAs("admin"), "ahmed@cu.edu.eg");

        Assert.Equal(HttpStatusCode.Accepted, (await ForgotAsync(Factory, email)).StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, (await ForgotAsync(Factory, "nobody@cu.edu.eg")).StatusCode);

        var sent = Assert.Single(await OutboxAsync(Factory, email));
        Assert.Contains("reset-password?token=", (string?)sent!["body"]);
        Assert.Empty(await OutboxAsync(Factory, "nobody@cu.edu.eg"));
    }

    [Fact]
    public async Task The_emailed_link_sets_a_new_password_once()
    {
        var (_, email) = await StudentAsync(Factory, Factory.CreateClientAs("admin"), "ahmed@cu.edu.eg");
        await ForgotAsync(Factory, email);
        var token = await LatestResetTokenAsync(Factory, email);

        Assert.Equal(HttpStatusCode.NoContent, (await ResetAsync(Factory, token, NewPassword)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(Factory, email, NewPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(Factory, email, Password)).StatusCode);
        var again = await ResetAsync(Factory, token, "Another!Pass2028");
        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);
        Assert.Contains("token", await FieldsOf(again));
    }

    [Fact]
    public async Task A_link_expires_after_thirty_minutes()
    {
        var (_, email) = await StudentAsync(Factory, Factory.CreateClientAs("admin"), "ahmed@cu.edu.eg");
        await ForgotAsync(Factory, email);
        var token = await LatestResetTokenAsync(Factory, email);

        Factory.Clock.Advance(TimeSpan.FromMinutes(31));

        Assert.Equal(HttpStatusCode.BadRequest, (await ResetAsync(Factory, token, NewPassword)).StatusCode);
    }

    [Fact]
    public async Task Only_the_newest_link_works_and_at_most_three_are_sent_an_hour()
    {
        var (_, email) = await StudentAsync(Factory, Factory.CreateClientAs("admin"), "ahmed@cu.edu.eg");
        await ForgotAsync(Factory, email);
        var first = await LatestResetTokenAsync(Factory, email);
        await ForgotAsync(Factory, email);
        var second = await LatestResetTokenAsync(Factory, email);
        await ForgotAsync(Factory, email);
        await ForgotAsync(Factory, email);

        Assert.Equal(3, (await OutboxAsync(Factory, email)).Count);
        Assert.Equal(HttpStatusCode.BadRequest, (await ResetAsync(Factory, first, NewPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await ResetAsync(Factory, second, NewPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await ResetAsync(Factory, await LatestResetTokenAsync(Factory, email), NewPassword)).StatusCode);
    }

    [Fact]
    public async Task A_reset_clears_a_lockout_and_ends_every_session()
    {
        var (_, email) = await StudentAsync(Factory, Factory.CreateClientAs("admin"), "ahmed@cu.edu.eg");
        var session = await SignedInAsync(Factory, email);
        for (var i = 0; i < 5; i++)
            await LoginAsync(Factory, email, "Wrong!Pass000");
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(Factory, email, Password)).StatusCode);

        await ForgotAsync(Factory, email);
        Assert.Equal(HttpStatusCode.NoContent, (await ResetAsync(Factory, await LatestResetTokenAsync(Factory, email), NewPassword)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(Factory, email, NewPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(Factory, (string)session["refreshToken"]!)).StatusCode);
    }

    [Fact]
    public async Task A_weak_new_password_is_rejected()
    {
        var (_, email) = await StudentAsync(Factory, Factory.CreateClientAs("admin"), "ahmed@cu.edu.eg");
        await ForgotAsync(Factory, email);

        var response = await ResetAsync(Factory, await LatestResetTokenAsync(Factory, email), "short");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("newPassword", await FieldsOf(response));
    }
}
