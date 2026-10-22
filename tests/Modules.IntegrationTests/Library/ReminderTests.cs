// ACCEPTANCE TEST — written by the consultant for phase P11. Do not modify; report problems instead.
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Library.LibrarySetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Library;

/// <summary>P11 Library (LIBREMIND) — due-soon (2 days) and overdue reminders, at most once per loan per day.</summary>
public sealed class ReminderTests(ReferenceHostFactory factory) : ReferenceHostTest(factory)
{
    [Fact]
    public async Task A_loan_due_within_two_days_gets_one_reminder_a_day()
    {
        var w = await BuildAsync(Factory, Admin());
        await BookWithCopiesAsync(Admin(), "Compilers", null, "LIB-0001");
        await LentAsync(Admin(), "LIB-0001", w.Ahmed);
        Factory.Clock.Advance(TimeSpan.FromDays(13));                     // due tomorrow
        var before = await UnreadAsync(w.AhmedClient);

        Assert.Equal(1, (int)(await RunRemindersAsync(Admin()))["dueSoon"]!);
        Assert.Equal(0, (int)(await RunRemindersAsync(Admin()))["dueSoon"]!);   // same day: no repeat

        Assert.Equal(before + 1, await UnreadAsync(w.AhmedClient));
        var latest = (await w.AhmedClient.GetFromJsonAsync<JsonObject>("/api/notifications/mine?pageSize=1"))!["items"]![0]!;
        Assert.Contains("Compilers", (string?)latest["message"]);
    }

    [Fact]
    public async Task An_overdue_loan_is_reminded_daily()
    {
        var w = await BuildAsync(Factory, Admin());
        await BookWithCopiesAsync(Admin(), "Compilers", null, "LIB-0001");
        await LentAsync(Admin(), "LIB-0001", w.Ahmed);

        Factory.Clock.Advance(TimeSpan.FromDays(16));
        Assert.Equal(1, (int)(await RunRemindersAsync(Admin()))["overdue"]!);
        Assert.Equal(0, (int)(await RunRemindersAsync(Admin()))["overdue"]!);

        Factory.Clock.Advance(TimeSpan.FromDays(1));
        Assert.Equal(1, (int)(await RunRemindersAsync(Admin()))["overdue"]!);
    }

    [Fact]
    public async Task Returned_or_far_off_loans_get_no_reminder()
    {
        var w = await BuildAsync(Factory, Admin());
        await BookWithCopiesAsync(Admin(), "Compilers", null, "LIB-0001");
        await BookWithCopiesAsync(Admin(), "Databases", null, "LIB-0002");
        var returned = (Guid)(await LentAsync(Admin(), "LIB-0001", w.Ahmed))["id"]!;
        await LentAsync(Admin(), "LIB-0002", w.Hany);                    // professors: 30 days
        await ReturnAsync(Admin(), returned);
        Factory.Clock.Advance(TimeSpan.FromDays(20));

        var run = await RunRemindersAsync(Admin());

        Assert.Equal(0, (int)run["dueSoon"]!);
        Assert.Equal(0, (int)run["overdue"]!);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");
}
