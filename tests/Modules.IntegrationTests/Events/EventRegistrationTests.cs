// ACCEPTANCE TEST — written by the consultant for phase P11. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Events.EventsSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Events;

/// <summary>P11 Events — members register themselves; capacity is atomic; cancelling frees the seat until the start.</summary>
public sealed class EventRegistrationTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task A_member_registers_and_sees_it_in_my_events()
    {
        var w = await BuildAsync(Factory, Admin());
        var id = await PublishedAsync(Admin(), Body(Factory, capacity: 10));

        var response = await RegisterAsync(w.AhmedClient, id);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("registered", (string?)(await BodyOf(response))["status"]);
        var shown = await EventAsync(w.AhmedClient, id);
        Assert.Equal("registered", (string?)shown["myStatus"]);
        Assert.Equal(9, (int)shown["seatsLeft"]!);
        var mine = (await w.AhmedClient.GetFromJsonAsync<JsonObject>("/api/events/mine"))!;
        var item = Assert.Single(mine["items"]!.AsArray())!;
        Assert.Equal(id, (Guid)item["id"]!);
        Assert.Equal("registered", (string?)item["myStatus"]);
    }

    [Fact]
    public async Task A_full_event_refuses_the_next_member()
    {
        var w = await BuildAsync(Factory, Admin());
        var id = await PublishedAsync(Admin(), Body(Factory, capacity: 1));

        Assert.Equal(HttpStatusCode.Created, (await RegisterAsync(w.AhmedClient, id)).StatusCode);
        var full = await RegisterAsync(w.MonaClient, id);

        Assert.Equal(HttpStatusCode.Conflict, full.StatusCode);
        Assert.Contains("full", (string?)(await BodyOf(full))["message"]);
        Assert.Equal(0, (int)(await EventAsync(w.MonaClient, id))["seatsLeft"]!);
    }

    [Fact]
    public async Task Concurrent_registrations_never_overbook()
    {
        var w = await BuildAsync(Factory, Admin());
        var id = await PublishedAsync(Admin(), Body(Factory, capacity: 2));
        var clients = new List<HttpClient> { w.AhmedClient, w.MonaClient, w.HanyClient };
        for (var i = 0; i < 3; i++)
        {
            var (_, token) = await AccountSetup.CreateStudentWithTokenAsync(Factory, Admin(), w.Cs, $"racer{i}@cu.edu.eg");
            clients.Add(Factory.CreateClientWithToken(token));
        }

        var results = await Task.WhenAll(clients.Select(c => RegisterAsync(c, id)));

        Assert.Equal(2, results.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.All(results.Where(r => r.StatusCode != HttpStatusCode.Created), r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
        var attendees = (await Admin().GetFromJsonAsync<JsonObject>($"/api/admin/events/{id}/registrations"))!;
        Assert.Equal(2, (int)attendees["totalCount"]!);
    }

    [Fact]
    public async Task Registration_closes_at_the_deadline()
    {
        var w = await BuildAsync(Factory, Admin());
        var id = await PublishedAsync(Admin(), Body(Factory));

        Factory.Clock.Advance(TimeSpan.FromDays(6).Add(TimeSpan.FromMinutes(1)));     // deadline = start − 1 day

        Assert.Equal(HttpStatusCode.Conflict, (await RegisterAsync(w.AhmedClient, id)).StatusCode);
    }

    [Fact]
    public async Task A_member_registers_once()
    {
        var w = await BuildAsync(Factory, Admin());
        var id = await PublishedAsync(Admin(), Body(Factory));
        await RegisterAsync(w.AhmedClient, id);

        Assert.Equal(HttpStatusCode.Conflict, (await RegisterAsync(w.AhmedClient, id)).StatusCode);
    }

    [Fact]
    public async Task Cancelling_frees_the_seat_until_the_event_starts()
    {
        var w = await BuildAsync(Factory, Admin());
        var id = await PublishedAsync(Admin(), Body(Factory, capacity: 1));
        await RegisterAsync(w.AhmedClient, id);

        Assert.Equal(HttpStatusCode.NoContent, (await w.AhmedClient.DeleteAsync($"/api/events/{id}/registrations/mine")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await RegisterAsync(w.MonaClient, id)).StatusCode);
        Assert.Equal("cancelled", (string?)(await EventAsync(w.AhmedClient, id))["myStatus"]);
        Assert.Equal(HttpStatusCode.NotFound, (await w.AhmedClient.DeleteAsync($"/api/events/{id}/registrations/mine")).StatusCode);

        Factory.Clock.Advance(TimeSpan.FromDays(7).Add(TimeSpan.FromMinutes(5)));       // the event has started
        Assert.Equal(HttpStatusCode.Conflict, (await w.MonaClient.DeleteAsync($"/api/events/{id}/registrations/mine")).StatusCode);
    }

    [Fact]
    public async Task A_member_can_register_again_after_cancelling()
    {
        var w = await BuildAsync(Factory, Admin());
        var id = await PublishedAsync(Admin(), Body(Factory, capacity: 5));
        await RegisterAsync(w.AhmedClient, id);
        await w.AhmedClient.DeleteAsync($"/api/events/{id}/registrations/mine");

        Assert.Equal(HttpStatusCode.Created, (await RegisterAsync(w.AhmedClient, id)).StatusCode);
        Assert.Equal(4, (int)(await EventAsync(w.AhmedClient, id))["seatsLeft"]!);
    }

    [Fact]
    public async Task Student_affairs_runs_events_but_does_not_register()
    {
        await BuildAsync(Factory, Admin());
        var id = await PublishedAsync(Admin(), Body(Factory));

        Assert.Equal(HttpStatusCode.Forbidden, (await RegisterAsync(Admin(), id)).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");
}
