// ACCEPTANCE TEST — written by the consultant for phase P11. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Events.EventsSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Events;

/// <summary>P11 Events — Student Affairs drafts, publishes, edits and cancels; members see published events of their audience.</summary>
public sealed class EventManagementTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task A_draft_is_invisible_until_published()
    {
        var w = await BuildAsync(Factory, Admin());

        var created = await CreateAsync(Admin(), Body(Factory, capacity: 50));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var draft = await BodyOf(created);
        var id = (Guid)draft["id"]!;
        Assert.Equal("draft", (string?)draft["status"]);
        Assert.Equal(HttpStatusCode.NotFound, (await w.AhmedClient.GetAsync($"/api/events/{id}")).StatusCode);
        Assert.Equal(0, (int)(await ListAsync(w.AhmedClient))["totalCount"]!);

        Assert.Equal(HttpStatusCode.OK, (await Admin().PostAsync($"/api/admin/events/{id}/publish", null)).StatusCode);

        var published = await EventAsync(w.AhmedClient, id);
        Assert.Equal("published", (string?)published["status"]);
        Assert.Equal("Career fair", (string?)published["title"]);
        Assert.Equal("Main hall", (string?)published["venue"]);
        Assert.Equal(50, (int)published["capacity"]!);
        Assert.Equal(50, (int)published["seatsLeft"]!);
        Assert.Null(published["myStatus"]);
        Assert.Equal([id], (await ListAsync(w.AhmedClient))["items"]!.AsArray().Select(e => (Guid)e!["id"]!));
    }

    [Fact]
    public async Task A_faculty_event_is_only_for_that_facultys_members()
    {
        var w = await BuildAsync(Factory, Admin());
        var id = await PublishedAsync(Admin(), Body(Factory, "ENG open day", facultyId: w.Eng));

        Assert.Equal(HttpStatusCode.OK, (await w.AhmedClient.GetAsync($"/api/events/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await w.SaraClient.GetAsync($"/api/events/{id}")).StatusCode);
        Assert.Equal(0, (int)(await ListAsync(w.SaraClient))["totalCount"]!);
        Assert.Equal(HttpStatusCode.NotFound, (await RegisterAsync(w.SaraClient, id)).StatusCode);
    }

    [Fact]
    public async Task Events_are_listed_soonest_first_and_past_ones_drop_off()
    {
        var w = await BuildAsync(Factory, Admin());
        var later = await PublishedAsync(Admin(), Body(Factory, "Later", startsIn: TimeSpan.FromDays(20)));
        var sooner = await PublishedAsync(Admin(), Body(Factory, "Sooner", startsIn: TimeSpan.FromDays(3)));

        Assert.Equal([sooner, later], (await ListAsync(w.AhmedClient))["items"]!.AsArray().Select(e => (Guid)e!["id"]!));

        Factory.Clock.Advance(TimeSpan.FromDays(4));
        Assert.Equal([later], (await ListAsync(w.AhmedClient))["items"]!.AsArray().Select(e => (Guid)e!["id"]!));
    }

    [Fact]
    public async Task Events_are_validated()
    {
        await BuildAsync(Factory, Admin());
        var now = Factory.Clock.GetUtcNow();

        var backwards = await CreateAsync(Admin(), new
        {
            title = "Backwards", description = "x", facultyId = (Guid?)null, venue = "Hall", startsAt = now.AddDays(5),
            endsAt = now.AddDays(4), capacity = (int?)null, registrationDeadline = now.AddDays(3),
        });
        Assert.Equal(HttpStatusCode.BadRequest, backwards.StatusCode);
        Assert.Contains("endsAt", await FieldsOf(backwards));

        var lateDeadline = await CreateAsync(Admin(), new
        {
            title = "Late", description = "x", facultyId = (Guid?)null, venue = "Hall", startsAt = now.AddDays(5),
            endsAt = now.AddDays(5).AddHours(1), capacity = (int?)null, registrationDeadline = now.AddDays(6),
        });
        Assert.Equal(HttpStatusCode.BadRequest, lateDeadline.StatusCode);
        Assert.Contains("registrationDeadline", await FieldsOf(lateDeadline));

        var noSeats = await CreateAsync(Admin(), Body(Factory, capacity: 0));
        Assert.Equal(HttpStatusCode.BadRequest, noSeats.StatusCode);
        Assert.Contains("capacity", await FieldsOf(noSeats));

        var noTitle = await CreateAsync(Admin(), Body(Factory, title: " "));
        Assert.Equal(HttpStatusCode.BadRequest, noTitle.StatusCode);
        Assert.Contains("title", await FieldsOf(noTitle));

        var unknownFaculty = await CreateAsync(Admin(), Body(Factory, facultyId: Guid.CreateVersion7()));
        Assert.Equal(HttpStatusCode.BadRequest, unknownFaculty.StatusCode);
        Assert.Contains("facultyId", await FieldsOf(unknownFaculty));
    }

    [Fact]
    public async Task A_cancelled_event_shows_why_and_cannot_be_edited_or_joined()
    {
        var w = await BuildAsync(Factory, Admin());
        var id = await PublishedAsync(Admin(), Body(Factory));

        var cancelled = await Admin().PostAsJsonAsync($"/api/admin/events/{id}/cancel", new { reason = "Venue unavailable" });

        Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);
        var shown = await EventAsync(w.AhmedClient, id);
        Assert.Equal("cancelled", (string?)shown["status"]);
        Assert.Equal("Venue unavailable", (string?)shown["cancellationReason"]);
        Assert.Equal(HttpStatusCode.Conflict, (await Admin().PutAsJsonAsync($"/api/admin/events/{id}", Body(Factory))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await RegisterAsync(w.AhmedClient, id)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Admin().PostAsync($"/api/admin/events/{id}/publish", null)).StatusCode);
    }

    [Fact]
    public async Task Student_affairs_sees_every_event_and_only_it_manages_them()
    {
        var w = await BuildAsync(Factory, Admin());
        await CreateAsync(Admin(), Body(Factory, "Draft one"));
        await PublishedAsync(Admin(), Body(Factory, "Live one"));

        var all = (await Admin().GetFromJsonAsync<JsonObject>("/api/admin/events"))!;
        Assert.Equal(2, (int)all["totalCount"]!);
        Assert.Equal(1, (int)(await Admin().GetFromJsonAsync<JsonObject>("/api/admin/events?status=draft"))!["totalCount"]!);
        Assert.Equal(HttpStatusCode.Forbidden, (await CreateAsync(w.HanyClient, Body(Factory))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await w.AhmedClient.GetAsync("/api/admin/events")).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private static async Task<JsonObject> ListAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<JsonObject>("/api/events"))!;
}
