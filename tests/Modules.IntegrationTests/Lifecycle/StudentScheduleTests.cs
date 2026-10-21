// ACCEPTANCE TEST — written by the consultant for phase P04A. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Lifecycle.RegistrationSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Lifecycle;

/// <summary>P4.5 — the student's own weekly schedule, built from their registrations. First week: 2026-09-19 … 2026-09-25.</summary>
public sealed class StudentScheduleTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    private const string FirstWeek = "/api/schedule?from=2026-09-19&to=2026-09-25";

    [Fact]
    public async Task A_student_sees_the_occurrences_of_their_registered_groups_in_order()
    {
        var w = await Campus();
        await RegisteredAsync(w.AhmedClient, w.Cs303Group1);
        await RegisteredAsync(w.AhmedClient, w.Cs301Group2);

        var week = await ScheduleAsync(w.AhmedClient, FirstWeek);

        Assert.Equal(["2026-09-21 11:00:00 CS301/2", "2026-09-22 09:00:00 CS303/1"], Summaries(week));
        var first = week[0]!;
        Assert.Equal(2, (int)first["dayOfWeek"]!);
        Assert.Equal("12:30:00", (string?)first["endTime"]);
        Assert.Equal("Algorithms", (string?)first["subjectName"]);
        Assert.Equal("الخوارزميات", (string?)first["subjectNameAr"]);
        Assert.Equal("Hall B", (string?)first["facilityName"]);
        Assert.NotNull((string?)first["scheduleId"]);
    }

    [Fact]
    public async Task Dropped_registrations_and_other_students_groups_are_not_shown()
    {
        var w = await Campus();
        var dropped = await RegisteredAsync(w.AhmedClient, w.Cs303Group1);
        await w.AhmedClient.DeleteAsync($"/api/registration/{dropped}");
        await RegisteredAsync(w.MonaClient, w.Cs301Group1);

        var week = await ScheduleAsync(w.AhmedClient, FirstWeek);

        Assert.Empty(week);
    }

    [Fact]
    public async Task The_range_rules_match_the_teaching_schedule()
    {
        var w = await Campus();

        Assert.Equal(HttpStatusCode.BadRequest, (await w.AhmedClient.GetAsync("/api/schedule?from=2026-09-19&to=2026-10-20")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await w.AhmedClient.GetAsync("/api/schedule?from=2026-09-25&to=2026-09-19")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await w.AhmedClient.GetAsync("/api/schedule?from=2026-09-19&to=2026-10-19")).StatusCode);
    }

    [Fact]
    public async Task Only_students_have_a_student_schedule()
    {
        var w = await Campus();

        Assert.Equal(HttpStatusCode.Forbidden, (await w.HanyClient.GetAsync(FirstWeek)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Factory.CreateClient().GetAsync(FirstWeek)).StatusCode);
    }

    private Task<CampusWorld> Campus() => BuildCampusAsync(Factory, Factory.CreateClientAs("admin"));

    private static async Task<JsonArray> ScheduleAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonArray>())!;
    }

    private static List<string> Summaries(JsonArray occurrences) =>
        occurrences.Select(o => $"{(string?)o!["date"]} {(string?)o["startTime"]} {(string?)o["subjectCode"]}/{(int)o["groupNumber"]!}").ToList();
}
