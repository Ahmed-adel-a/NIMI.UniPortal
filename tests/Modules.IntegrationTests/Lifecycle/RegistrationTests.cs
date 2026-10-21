// ACCEPTANCE TEST — written by the consultant for phase P04A. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Lifecycle.RegistrationSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Lifecycle;

/// <summary>P4.3 — self-registration and drop (decision D2, rules R1–R10). Campus: see <see cref="CampusWorld"/>.</summary>
public sealed class RegistrationTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task A_student_registers_a_group_in_the_open_window()
    {
        var w = await Campus();

        var response = await RegisterAsync(w.AhmedClient, w.Cs301Group1);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var registration = await BodyOf(response);
        Assert.Equal(w.Cs301, (Guid)registration["subjectId"]!);
        Assert.Equal("CS301", (string?)registration["subjectCode"]);
        Assert.Equal(w.Cs301Group1, (Guid)registration["groupId"]!);
        Assert.Equal(1, (int)registration["groupNumber"]!);
        Assert.Equal(3, (int)registration["creditHours"]!);
        Assert.Equal("registered", (string?)registration["status"]);
        Assert.NotNull((string?)registration["registeredAt"]);

        var mine = await MineAsync(w.AhmedClient);
        Assert.Equal(w.TermId, (Guid)mine["termId"]!);
        Assert.Single(mine["registrations"]!.AsArray());
        Assert.Equal("الخوارزميات", (string?)mine["registrations"]![0]!["subjectNameAr"]);
        Assert.Equal(3, (int)mine["totalCredits"]!);
        Assert.Equal(12, (int)mine["minCredits"]!);
        Assert.Equal(21, (int)mine["maxCredits"]!);
        Assert.True((bool)mine["belowMinimum"]!);
    }

    [Fact]
    public async Task Registration_outside_the_window_is_closed()
    {
        var w = await Campus();
        var now = Factory.Clock.GetUtcNow();
        Assert.Equal(HttpStatusCode.OK, (await PutWindowAsync(Admin(), w.WindowId, now.AddDays(1), now.AddDays(8))).StatusCode);

        var response = await RegisterAsync(w.AhmedClient, w.Cs301Group1);

        await AssertRule(response, "Registration is closed.");
    }

    [Fact]
    public async Task Registration_closes_when_the_window_ends()
    {
        var w = await Campus();
        var now = Factory.Clock.GetUtcNow();
        await PutWindowAsync(Admin(), w.WindowId, now.AddHours(-2), now.AddHours(1));

        Factory.Clock.Advance(TimeSpan.FromHours(2));
        var response = await RegisterAsync(w.AhmedClient, w.Cs301Group1);

        await AssertRule(response, "Registration is closed.");
    }

    [Fact]
    public async Task An_inactive_group_or_a_group_of_another_term_is_not_offered()
    {
        var w = await Campus();
        await Admin().PutAsJsonAsync($"/api/admin/groups/{w.Cs301Group2}", new { capacity = 60, targetDepartmentId = (Guid?)null, isActive = false });
        var nextTerm = await CreateTermAsync(Admin(), number: "second", startsOn: "2027-02-06", endsOn: "2027-06-04", weekCount: 17);
        var nextTermGroup = await CreateGroupAsync(Admin(), w.Cs303, nextTerm, 1);

        await AssertRule(await RegisterAsync(w.AhmedClient, w.Cs301Group2), "This group is not offered.");
        await AssertRule(await RegisterAsync(w.AhmedClient, nextTermGroup), "This group is not offered.");
    }

    [Fact]
    public async Task A_group_reserved_for_another_department_is_refused()
    {
        var w = await Campus();

        var response = await RegisterAsync(w.AhmedClient, w.Civ101Group1);

        await AssertRule(response, "This group is reserved for another department.");
    }

    [Fact]
    public async Task A_second_group_of_the_same_subject_is_a_409()
    {
        var w = await Campus();
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);

        var response = await RegisterAsync(w.AhmedClient, w.Cs301Group2);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Military_education_cannot_be_self_registered()
    {
        var w = await Campus();

        var response = await RegisterAsync(w.AhmedClient, w.Mil100Group1);

        await AssertRule(response, "Registered through the military-education process.");
    }

    [Fact]
    public async Task A_missing_prerequisite_is_named()
    {
        var w = await Campus();
        Assert.Equal(HttpStatusCode.OK, (await SetPrerequisitesAsync(Admin(), w.Cs303, w.Cs301)).StatusCode);

        var response = await RegisterAsync(w.AhmedClient, w.Cs303Group1);

        await AssertRule(response, "Missing prerequisite: CS301.");
    }

    [Fact]
    public async Task The_window_maximum_cannot_be_exceeded()
    {
        var w = await Campus();
        var now = Factory.Clock.GetUtcNow();
        await PutWindowAsync(Admin(), w.WindowId, now.AddHours(-1), now.AddDays(7), minCredits: 0, maxCredits: 3);
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);

        var response = await RegisterAsync(w.AhmedClient, w.Cs303Group1);

        await AssertRule(response, "Exceeds the maximum of 3 credit hours.");
    }

    [Fact]
    public async Task A_time_clash_with_another_registered_group_is_named()
    {
        var w = await Campus();
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);

        var response = await RegisterAsync(w.AhmedClient, w.Cs302Group1);

        await AssertRule(response, "Time clash with CS301 group 1.");
    }

    [Fact]
    public async Task A_full_group_is_a_409()
    {
        var w = await Campus();
        await RegisteredAsync(w.MonaClient, w.Cs303Group1);

        var response = await RegisterAsync(w.AhmedClient, w.Cs303Group1);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("The group is full.", (string?)(await BodyOf(response))["message"]);
    }

    [Fact]
    public async Task Two_students_racing_for_the_last_seat_get_exactly_one_seat()
    {
        var w = await Campus();

        var responses = await Task.WhenAll(RegisterAsync(w.AhmedClient, w.Cs303Group1), RegisterAsync(w.MonaClient, w.Cs303Group1));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Dropping_frees_the_seat_and_the_subject_can_be_registered_again()
    {
        var w = await Campus();
        var registration = await RegisteredAsync(w.AhmedClient, w.Cs303Group1);

        var drop = await w.AhmedClient.DeleteAsync($"/api/registration/{registration}");

        Assert.Equal(HttpStatusCode.NoContent, drop.StatusCode);
        Assert.Empty((await MineAsync(w.AhmedClient))["registrations"]!.AsArray());
        Assert.Equal(HttpStatusCode.Created, (await RegisterAsync(w.MonaClient, w.Cs303Group1)).StatusCode);     // the seat came back
        Assert.Equal(HttpStatusCode.Created, (await RegisterAsync(w.AhmedClient, w.Cs301Group1)).StatusCode);
    }

    [Fact]
    public async Task A_student_can_register_a_subject_again_after_dropping_it()
    {
        var w = await Campus();
        var first = await RegisteredAsync(w.AhmedClient, w.Cs301Group1);
        await w.AhmedClient.DeleteAsync($"/api/registration/{first}");

        var again = await RegisterAsync(w.AhmedClient, w.Cs301Group2);

        Assert.Equal(HttpStatusCode.Created, again.StatusCode);
    }

    [Fact]
    public async Task Someone_elses_or_an_unknown_registration_cannot_be_dropped()
    {
        var w = await Campus();
        var monas = await RegisteredAsync(w.MonaClient, w.Cs301Group1);

        var someoneElses = await w.AhmedClient.DeleteAsync($"/api/registration/{monas}");
        var unknown = await w.AhmedClient.DeleteAsync($"/api/registration/{Guid.CreateVersion7()}");

        Assert.Equal(HttpStatusCode.NotFound, someoneElses.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Single((await MineAsync(w.MonaClient))["registrations"]!.AsArray());
    }

    [Fact]
    public async Task Dropping_after_the_window_closed_is_a_400()
    {
        var w = await Campus();
        var registration = await RegisteredAsync(w.AhmedClient, w.Cs301Group1);
        var now = Factory.Clock.GetUtcNow();
        await PutWindowAsync(Admin(), w.WindowId, now.AddDays(-7), now.AddMinutes(-1));

        var drop = await w.AhmedClient.DeleteAsync($"/api/registration/{registration}");

        Assert.Equal(HttpStatusCode.BadRequest, drop.StatusCode);
        Assert.Contains("registrationId", await FieldsOf(drop));
    }

    [Fact]
    public async Task An_unknown_group_is_a_400_on_subjectGroupId()
    {
        var w = await Campus();

        var response = await RegisterAsync(w.AhmedClient, Guid.CreateVersion7());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("subjectGroupId", await FieldsOf(response));
    }

    [Fact]
    public async Task Only_students_register()
    {
        var w = await Campus();

        Assert.Equal(HttpStatusCode.Forbidden, (await RegisterAsync(w.HanyClient, w.Cs301Group1)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await RegisterAsync(Admin(), w.Cs301Group1)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RegisterAsync(Factory.CreateClient(), w.Cs301Group1)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await w.HanyClient.GetAsync("/api/registration/mine")).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private Task<CampusWorld> Campus() => BuildCampusAsync(Factory, Admin());

    private static async Task AssertRule(HttpResponseMessage response, string message)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("subjectGroupId", await FieldsOf(response));
        Assert.Contains(message, await MessagesOf(response));
    }
}
