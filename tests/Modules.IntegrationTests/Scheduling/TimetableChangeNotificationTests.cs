// ACCEPTANCE TEST — written by the consultant for phase P10. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Lifecycle;
using static NIMI.UniPortal.Modules.IntegrationTests.Lifecycle.RegistrationSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Scheduling;

/// <summary>
/// P10.3 — editing or cancelling a timetable slot notifies the group's confirmed students and the slot's staff (old and
/// new), type <c>schedule</c>, with old → new day, time and hall in both languages. An edit that changes nothing is silent.
/// </summary>
public sealed class TimetableChangeNotificationTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    private const int Tuesday = 3;

    [Fact]
    public async Task Moving_a_slot_notifies_its_students_and_staff_only()
    {
        var (w, slot, hall) = await SlotAsync();
        var before = await CountsAsync(w);

        var moved = await Admin().PutAsJsonAsync($"/api/admin/schedules/{slot}", SlotBody(w.Cs301Group2, hall, w.Hany, Tuesday, "12:00:00", "13:00:00"));

        Assert.Equal(HttpStatusCode.OK, moved.StatusCode);
        var after = await CountsAsync(w);
        Assert.Equal(before.Ahmed + 1, after.Ahmed);        // registered in group 2
        Assert.Equal(before.Hany + 1, after.Hany);          // the slot's staff
        Assert.Equal(before.Mona, after.Mona);              // group 1 — not affected
        var latest = await LatestAsync(w.AhmedClient);
        Assert.Equal("schedule", (string?)latest["type"]);
        Assert.Contains("CS301", (string?)latest["title"]);
        Assert.Contains("11:00", (string?)latest["message"]);
        Assert.Contains("12:00", (string?)latest["message"]);
        Assert.Contains("12:00", (string?)latest["messageAr"]);
    }

    [Fact]
    public async Task Cancelling_a_slot_notifies_its_students()
    {
        var (w, slot, _) = await SlotAsync();
        var before = await CountsAsync(w);

        Assert.Equal(HttpStatusCode.NoContent, (await Admin().PostAsync($"/api/admin/schedules/{slot}/cancel", null)).StatusCode);

        var after = await CountsAsync(w);
        Assert.Equal(before.Ahmed + 1, after.Ahmed);
        Assert.Equal(before.Hany + 1, after.Hany);
        Assert.Contains("CS301", (string?)(await LatestAsync(w.AhmedClient))["title"]);
    }

    [Fact]
    public async Task Changing_the_staff_tells_both_the_old_and_the_new_lecturer()
    {
        var (w, slot, hall) = await SlotAsync();
        var (sami, samiToken) = await AccountSetup.CreateProfessorWithTokenAsync(Factory, Admin(), w.CsDepartmentId, "sami@cu.edu.eg");
        var samiClient = Factory.CreateClientWithToken(samiToken);
        var hanyBefore = await UnreadAsync(w.HanyClient);
        var samiBefore = await UnreadAsync(samiClient);

        var moved = await Admin().PutAsJsonAsync($"/api/admin/schedules/{slot}", SlotBody(w.Cs301Group2, hall, sami, Tuesday, "11:00:00", "12:00:00"));

        Assert.Equal(HttpStatusCode.OK, moved.StatusCode);
        Assert.Equal(hanyBefore + 1, await UnreadAsync(w.HanyClient));
        Assert.Equal(samiBefore + 1, await UnreadAsync(samiClient));
    }

    [Fact]
    public async Task An_edit_that_changes_nothing_notifies_no_one()
    {
        var (w, slot, hall) = await SlotAsync();
        var before = await CountsAsync(w);

        var same = await Admin().PutAsJsonAsync($"/api/admin/schedules/{slot}", SlotBody(w.Cs301Group2, hall, w.Hany, Tuesday, "11:00:00", "12:00:00"));

        Assert.Equal(HttpStatusCode.OK, same.StatusCode);
        Assert.Equal(before, await CountsAsync(w));
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    /// <summary>The campus plus a Tuesday 11:00–12:00 slot for CS301 group 2 in Hall C taught by Hany; Ahmed is in group 2, Mona in group 1.</summary>
    private async Task<(CampusWorld World, Guid Slot, Guid Hall)> SlotAsync()
    {
        var w = await BuildCampusAsync(Factory, Admin());
        await RegisteredAsync(w.AhmedClient, w.Cs301Group2);
        await RegisteredAsync(w.MonaClient, w.Cs301Group1);
        var hall = await CreateFacilityAsync(Admin(), "Hall C", w.FacultyId);
        var slot = await CreateSlotAsync(Admin(), w.Cs301Group2, hall, w.Hany, Tuesday, "11:00:00", "12:00:00");
        return (w, slot, hall);
    }

    private static async Task<(int Ahmed, int Mona, int Hany)> CountsAsync(CampusWorld w) =>
        (await UnreadAsync(w.AhmedClient), await UnreadAsync(w.MonaClient), await UnreadAsync(w.HanyClient));

    private static async Task<int> UnreadAsync(HttpClient client) =>
        (int)(await client.GetFromJsonAsync<JsonObject>("/api/notifications/mine/unread-count"))!["count"]!;

    private static async Task<JsonNode> LatestAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<JsonObject>("/api/notifications/mine?pageSize=1"))!["items"]![0]!;
}
