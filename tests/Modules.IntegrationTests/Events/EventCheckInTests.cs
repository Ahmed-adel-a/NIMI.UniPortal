// ACCEPTANCE TEST — written by the consultant for phase P11. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Events.EventsSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Events;

/// <summary>P11 Events — Student Affairs checks registered members in, from one hour before the start until the end.</summary>
public sealed class EventCheckInTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task Registered_members_are_checked_in_during_the_event()
    {
        var w = await BuildAsync(Factory, Admin());
        var id = await PublishedAsync(Admin(), Body(Factory));
        await RegisterAsync(w.AhmedClient, id);
        await RegisterAsync(w.MonaClient, id);

        Assert.Equal(HttpStatusCode.Conflict, (await CheckInAsync(id, w.Ahmed)).StatusCode);        // a week early

        Factory.Clock.Advance(TimeSpan.FromDays(7).Subtract(TimeSpan.FromMinutes(30)));           // 30 min before the start
        var checkedIn = await CheckInAsync(id, w.Ahmed);
        Assert.Equal(HttpStatusCode.OK, checkedIn.StatusCode);
        Assert.Equal("attended", (string?)(await BodyOf(checkedIn))["status"]);
        Assert.Equal(HttpStatusCode.Conflict, (await CheckInAsync(id, w.Ahmed)).StatusCode);        // only once

        var list = (await Admin().GetFromJsonAsync<JsonObject>($"/api/admin/events/{id}/registrations"))!;
        var rows = list["items"]!.AsArray();
        Assert.Equal("attended", (string?)rows.Single(r => (Guid)r!["userId"]! == w.Ahmed)!["status"]);
        Assert.Equal("registered", (string?)rows.Single(r => (Guid)r!["userId"]! == w.Mona)!["status"]);
        Assert.NotNull(rows[0]!["fullName"]);
        Assert.Equal("attended", (string?)(await EventAsync(w.AhmedClient, id))["myStatus"]);
    }

    [Fact]
    public async Task Only_registered_members_are_checked_in_and_not_after_the_end()
    {
        var w = await BuildAsync(Factory, Admin());
        var id = await PublishedAsync(Admin(), Body(Factory));
        await RegisterAsync(w.AhmedClient, id);
        Factory.Clock.Advance(TimeSpan.FromDays(7));

        Assert.Equal(HttpStatusCode.NotFound, (await CheckInAsync(id, w.Mona)).StatusCode);

        Factory.Clock.Advance(TimeSpan.FromHours(3));                                              // ended an hour ago
        Assert.Equal(HttpStatusCode.Conflict, (await CheckInAsync(id, w.Ahmed)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await w.HanyClient.PostAsJsonAsync($"/api/admin/events/{id}/check-in", new { userId = w.Ahmed })).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private Task<HttpResponseMessage> CheckInAsync(Guid eventId, Guid userId) =>
        Admin().PostAsJsonAsync($"/api/admin/events/{eventId}/check-in", new { userId });
}
