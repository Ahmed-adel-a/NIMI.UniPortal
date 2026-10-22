// ACCEPTANCE TEST — written by the consultant for phase P11. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Events.EventsSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Events;

/// <summary>
/// P11 Events (R4/R5) — a new time or venue of a published event, or its cancellation, notifies the registered members
/// (type <c>event</c>); publishing and text-only edits notify no one.
/// </summary>
public sealed class EventNotificationTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task A_new_venue_notifies_registered_members_only()
    {
        var w = await BuildAsync(Factory, Admin());
        var id = await PublishedAsync(Admin(), Body(Factory));
        await RegisterAsync(w.AhmedClient, id);
        var ahmed = await UnreadAsync(w.AhmedClient);
        var mona = await UnreadAsync(w.MonaClient);

        var moved = await Admin().PutAsJsonAsync($"/api/admin/events/{id}", Body(Factory, venue: "Hall 7"));

        Assert.Equal(HttpStatusCode.OK, moved.StatusCode);
        Assert.Equal(ahmed + 1, await UnreadAsync(w.AhmedClient));
        Assert.Equal(mona, await UnreadAsync(w.MonaClient));
        var latest = (await w.AhmedClient.GetFromJsonAsync<JsonObject>("/api/notifications/mine?pageSize=1"))!["items"]![0]!;
        Assert.Equal("event", (string?)latest["type"]);
        Assert.Contains("Hall 7", (string?)latest["message"]);
    }

    [Fact]
    public async Task A_text_only_edit_notifies_no_one()
    {
        var w = await BuildAsync(Factory, Admin());
        var body = Body(Factory);
        var id = await PublishedAsync(Admin(), body);
        await RegisterAsync(w.AhmedClient, id);
        var before = await UnreadAsync(w.AhmedClient);
        var current = await EventAsync(Admin(), id);

        var edited = await Admin().PutAsJsonAsync($"/api/admin/events/{id}", new
        {
            title = "Career fair 2026", description = "Meet 50 companies.", facultyId = (Guid?)null, venue = "Main hall",
            startsAt = (DateTimeOffset)current["startsAt"]!, endsAt = (DateTimeOffset)current["endsAt"]!, capacity = (int?)null,
            registrationDeadline = (DateTimeOffset)current["registrationDeadline"]!,
        });

        Assert.Equal(HttpStatusCode.OK, edited.StatusCode);
        Assert.Equal(before, await UnreadAsync(w.AhmedClient));
    }

    [Fact]
    public async Task Cancelling_notifies_registered_members_with_the_reason()
    {
        var w = await BuildAsync(Factory, Admin());
        var id = await PublishedAsync(Admin(), Body(Factory));
        await RegisterAsync(w.AhmedClient, id);
        await RegisterAsync(w.MonaClient, id);
        var ahmed = await UnreadAsync(w.AhmedClient);
        var hany = await UnreadAsync(w.HanyClient);

        await Admin().PostAsJsonAsync($"/api/admin/events/{id}/cancel", new { reason = "Venue unavailable" });

        Assert.Equal(ahmed + 1, await UnreadAsync(w.AhmedClient));
        Assert.Equal(hany, await UnreadAsync(w.HanyClient));
        var latest = (await w.AhmedClient.GetFromJsonAsync<JsonObject>("/api/notifications/mine?pageSize=1"))!["items"]![0]!;
        Assert.Contains("Venue unavailable", (string?)latest["message"]);
    }

    [Fact]
    public async Task Publishing_notifies_no_one()
    {
        var w = await BuildAsync(Factory, Admin());
        var before = await UnreadAsync(w.AhmedClient);

        await PublishedAsync(Admin(), Body(Factory));

        Assert.Equal(before, await UnreadAsync(w.AhmedClient));
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");
}
