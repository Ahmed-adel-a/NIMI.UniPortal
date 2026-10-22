// ACCEPTANCE TEST — written by the consultant for phase P11. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Users;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Events;

/// <summary>Events helpers through the real API (P11, decision EVENTSDESIGN).</summary>
internal static class EventsSetup
{
    /// <summary>ENG faculty (CS: students Ahmed, Mona; professor Hany) and COM faculty (ACC: student Sara).</summary>
    public static async Task<EventsWorld> BuildAsync(PortalWebAppFactory factory, HttpClient admin)
    {
        var eng = await OrganizationSetup.CreateFacultyAsync(admin, "ENG");
        var cs = await OrganizationSetup.CreateDepartmentAsync(admin, eng, "CS");
        var com = await OrganizationSetup.CreateFacultyAsync(admin, "COM");
        var acc = await OrganizationSetup.CreateDepartmentAsync(admin, com, "ACC");
        var (ahmed, ahmedToken) = await AccountSetup.CreateStudentWithTokenAsync(factory, admin, cs, "ahmed@cu.edu.eg");
        var (mona, monaToken) = await AccountSetup.CreateStudentWithTokenAsync(factory, admin, cs, "mona@cu.edu.eg");
        var (hany, hanyToken) = await AccountSetup.CreateProfessorWithTokenAsync(factory, admin, cs, "hany@cu.edu.eg");
        var (sara, saraToken) = await AccountSetup.CreateStudentWithTokenAsync(factory, admin, acc, "sara@cu.edu.eg");
        return new EventsWorld(
            eng, cs, com,
            ahmed, factory.CreateClientWithToken(ahmedToken),
            mona, factory.CreateClientWithToken(monaToken),
            hany, factory.CreateClientWithToken(hanyToken),
            sara, factory.CreateClientWithToken(saraToken));
    }

    /// <summary>The event body: starts in 7 days for 2 hours, registration closes the day before.</summary>
    public static object Body(
        PortalWebAppFactory factory, string title = "Career fair", Guid? facultyId = null, int? capacity = null,
        string venue = "Main hall", TimeSpan? startsIn = null, string description = "Meet 40 companies.")
    {
        var startsAt = factory.Clock.GetUtcNow().Add(startsIn ?? TimeSpan.FromDays(7));
        return new
        {
            title, description, facultyId, venue, startsAt, endsAt = startsAt.AddHours(2), capacity,
            registrationDeadline = startsAt.AddDays(-1),
        };
    }

    public static async Task<HttpResponseMessage> CreateAsync(HttpClient admin, object body) =>
        await admin.PostAsJsonAsync("/api/admin/events", body);

    /// <summary>Creates and publishes an event; returns its id.</summary>
    public static async Task<Guid> PublishedAsync(HttpClient admin, object body)
    {
        var created = await CreateAsync(admin, body);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (Guid)(await BodyOf(created))["id"]!;
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/admin/events/{id}/publish", null)).StatusCode);
        return id;
    }

    public static async Task<HttpResponseMessage> RegisterAsync(HttpClient member, Guid eventId) =>
        await member.PostAsync($"/api/events/{eventId}/registrations", null);

    public static async Task<JsonObject> EventAsync(HttpClient client, Guid eventId) =>
        (await client.GetFromJsonAsync<JsonObject>($"/api/events/{eventId}"))!;

    public static async Task<int> UnreadAsync(HttpClient client) =>
        (int)(await client.GetFromJsonAsync<JsonObject>("/api/notifications/mine/unread-count"))!["count"]!;
}
