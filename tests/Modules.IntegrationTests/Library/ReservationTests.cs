// ACCEPTANCE TEST — written by the consultant for phase P11. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Library.LibrarySetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Library;

/// <summary>
/// P11 Library — members queue for a book with no copy on the shelf; a returned copy is held for the first in line
/// (notified) for the hold period, then passes on.
/// </summary>
public sealed class ReservationTests(ReferenceHostFactory factory) : ReferenceHostTest(factory)
{
    [Fact]
    public async Task Members_queue_only_when_no_copy_is_available()
    {
        var w = await BuildAsync(Factory, Admin());
        var bookId = await BookWithCopiesAsync(Admin(), "Compilers", null, "LIB-0001");

        Assert.Equal(HttpStatusCode.Conflict, (await ReserveAsync(w.MonaClient, bookId)).StatusCode);   // borrow it instead

        await LentAsync(Admin(), "LIB-0001", w.Ahmed);
        var first = await ReserveAsync(w.MonaClient, bookId);
        var second = await ReserveAsync(w.HanyClient, bookId);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(1, (int)(await BodyOf(first))["position"]!);
        Assert.Equal(2, (int)(await BodyOf(second))["position"]!);
        Assert.Equal("waiting", (string?)(await BodyOf(first))["status"]);
        Assert.Equal(HttpStatusCode.Conflict, (await ReserveAsync(w.MonaClient, bookId)).StatusCode);    // once
        Assert.Equal(HttpStatusCode.Conflict, (await ReserveAsync(w.AhmedClient, bookId)).StatusCode);   // has it already
        Assert.Equal(2, (int)(await BookAsync(w.AhmedClient, bookId))["waiting"]!);
    }

    [Fact]
    public async Task A_returned_copy_is_held_for_the_first_in_line()
    {
        var w = await BuildAsync(Factory, Admin());
        var bookId = await BookWithCopiesAsync(Admin(), "Compilers", null, "LIB-0001");
        var loanId = (Guid)(await LentAsync(Admin(), "LIB-0001", w.Ahmed))["id"]!;
        await ReserveAsync(w.MonaClient, bookId);
        await ReserveAsync(w.HanyClient, bookId);
        var monaBefore = await UnreadAsync(w.MonaClient);

        await ReturnAsync(Admin(), loanId);

        Assert.Equal(monaBefore + 1, await UnreadAsync(w.MonaClient));
        Assert.Equal("on_hold", (string?)(await BookAsync(w.MonaClient, bookId))["copies"]![0]!["status"]);
        var mine = (await w.MonaClient.GetFromJsonAsync<JsonObject>("/api/library/reservations/mine"))!["items"]![0]!;
        Assert.Equal("ready", (string?)mine["status"]);
        Assert.NotNull(mine["expiresAt"]);
        Assert.Equal(HttpStatusCode.Conflict, (await LendAsync(Admin(), "LIB-0001", w.Hany)).StatusCode);   // held for Mona
        Assert.Equal(HttpStatusCode.Created, (await LendAsync(Admin(), "LIB-0001", w.Mona)).StatusCode);
        var fulfilled = (await w.MonaClient.GetFromJsonAsync<JsonObject>("/api/library/reservations/mine"))!["items"]![0]!;
        Assert.Equal("fulfilled", (string?)fulfilled["status"]);
    }

    [Fact]
    public async Task An_uncollected_hold_expires_and_passes_to_the_next_member()
    {
        var w = await BuildAsync(Factory, Admin());
        var bookId = await BookWithCopiesAsync(Admin(), "Compilers", null, "LIB-0001");
        var loanId = (Guid)(await LentAsync(Admin(), "LIB-0001", w.Ahmed))["id"]!;
        await ReserveAsync(w.MonaClient, bookId);
        await ReserveAsync(w.HanyClient, bookId);
        await ReturnAsync(Admin(), loanId);
        var hanyBefore = await UnreadAsync(w.HanyClient);

        Factory.Clock.Advance(TimeSpan.FromDays(4));                     // hold period: 3 days
        var run = await RunRemindersAsync(Admin());

        Assert.Equal(1, (int)run["expiredHolds"]!);
        Assert.Equal("expired", (string?)(await w.MonaClient.GetFromJsonAsync<JsonObject>("/api/library/reservations/mine"))!["items"]![0]!["status"]);
        Assert.Equal("ready", (string?)(await w.HanyClient.GetFromJsonAsync<JsonObject>("/api/library/reservations/mine"))!["items"]![0]!["status"]);
        Assert.Equal(hanyBefore + 1, await UnreadAsync(w.HanyClient));
    }

    [Fact]
    public async Task A_member_cancels_their_own_reservation()
    {
        var w = await BuildAsync(Factory, Admin());
        var bookId = await BookWithCopiesAsync(Admin(), "Compilers", null, "LIB-0001");
        await LentAsync(Admin(), "LIB-0001", w.Ahmed);
        var reservationId = (Guid)(await BodyOf(await ReserveAsync(w.MonaClient, bookId)))["id"]!;

        Assert.Equal(HttpStatusCode.NotFound, (await w.HanyClient.DeleteAsync($"/api/library/reservations/{reservationId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await w.MonaClient.DeleteAsync($"/api/library/reservations/{reservationId}")).StatusCode);
        Assert.Equal(0, (int)(await BookAsync(w.MonaClient, bookId))["waiting"]!);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");
}
