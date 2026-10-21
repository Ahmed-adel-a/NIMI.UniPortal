// ACCEPTANCE TEST — written by the consultant for phase P04A. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Users;
using static NIMI.UniPortal.Modules.IntegrationTests.Lifecycle.RegistrationSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Lifecycle;

/// <summary>P4.3 — what a student can register this term, and why not when they can't.</summary>
public sealed class OfferingsTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task Offerings_list_the_current_terms_groups_with_slots_and_seats()
    {
        var w = await Campus();

        var page = await OfferingsAsync(w.AhmedClient);

        Assert.Equal(6, (int)page["totalCount"]!);
        Assert.Equal(
            ["CIV101/1", "CS301/1", "CS301/2", "CS302/1", "CS303/1", "MIL100/1"],
            Items(page).Select(o => $"{(string?)o["subjectCode"]}/{(int)o["groupNumber"]!}"));
        var cs301Group1 = Offering(page, w.Cs301Group1);
        Assert.Equal(w.Cs301, (Guid)cs301Group1["subjectId"]!);
        Assert.Equal("Algorithms", (string?)cs301Group1["subjectName"]);
        Assert.Equal("الخوارزميات", (string?)cs301Group1["subjectNameAr"]);
        Assert.Equal(3, (int)cs301Group1["creditHours"]!);
        Assert.Equal(60, (int)cs301Group1["capacity"]!);
        Assert.Equal(60, (int)cs301Group1["seatsLeft"]!);
        var slot = cs301Group1["slots"]!.AsArray().Single()!;
        Assert.Equal(1, (int)slot["dayOfWeek"]!);
        Assert.Equal("09:00:00", (string?)slot["startTime"]);
        Assert.Equal("10:30:00", (string?)slot["endTime"]);
        Assert.Equal("Hall A", (string?)slot["facilityName"]);
    }

    [Fact]
    public async Task Each_offering_says_whether_this_student_can_register_and_why_not()
    {
        var w = await Campus();
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);

        var page = await OfferingsAsync(w.AhmedClient);

        Assert.True((bool)Offering(page, w.Cs303Group1)["eligible"]!);
        Assert.Empty(Offering(page, w.Cs303Group1)["reasons"]!.AsArray());
        AssertNotEligible(Offering(page, w.Civ101Group1), "This group is reserved for another department.");
        AssertNotEligible(Offering(page, w.Mil100Group1), "Registered through the military-education process.");
        AssertNotEligible(Offering(page, w.Cs301Group2), "Already registered for this subject.");
        AssertNotEligible(Offering(page, w.Cs302Group1), "Time clash with CS301 group 1.");
    }

    [Fact]
    public async Task Seats_go_down_as_students_register_and_a_full_group_says_so()
    {
        var w = await Campus();
        await RegisteredAsync(w.MonaClient, w.Cs303Group1);
        await RegisteredAsync(w.MonaClient, w.Cs301Group1);

        var page = await OfferingsAsync(w.AhmedClient);

        Assert.Equal(59, (int)Offering(page, w.Cs301Group1)["seatsLeft"]!);
        Assert.Equal(0, (int)Offering(page, w.Cs303Group1)["seatsLeft"]!);
        AssertNotEligible(Offering(page, w.Cs303Group1), "The group is full.");
    }

    [Fact]
    public async Task Outside_the_window_every_offering_explains_that_registration_is_closed()
    {
        var w = await Campus();
        var now = Factory.Clock.GetUtcNow();
        await PutWindowAsync(Admin(), w.WindowId, now.AddDays(1), now.AddDays(8));

        var page = await OfferingsAsync(w.AhmedClient);

        Assert.All(Items(page), o => Assert.Contains("Registration is closed.", o["reasons"]!.AsArray().Select(r => (string?)r)));
    }

    [Fact]
    public async Task Without_an_active_term_there_are_no_offerings()
    {
        var cs = await OrganizationSetup.CreateDepartmentAsync(Admin(), await OrganizationSetup.CreateFacultyAsync(Admin(), "ENG"), "CS");
        var (_, token) = await AccountSetup.CreateStudentWithTokenAsync(Factory, Admin(), cs, "ahmed@cu.edu.eg");

        var response = await Factory.CreateClientWithToken(token).GetAsync("/api/registration/offerings");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Only_students_see_offerings()
    {
        var w = await Campus();

        Assert.Equal(HttpStatusCode.Forbidden, (await w.HanyClient.GetAsync("/api/registration/offerings")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Factory.CreateClient().GetAsync("/api/registration/offerings")).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private Task<CampusWorld> Campus() => BuildCampusAsync(Factory, Admin());

    private static async Task<JsonObject> OfferingsAsync(HttpClient student) =>
        (await student.GetFromJsonAsync<JsonObject>("/api/registration/offerings?pageSize=100"))!;

    private static IEnumerable<JsonNode> Items(JsonObject page) => page["items"]!.AsArray().Select(i => i!);

    private static JsonNode Offering(JsonObject page, Guid groupId) => Items(page).Single(o => (Guid)o["groupId"]! == groupId);

    private static void AssertNotEligible(JsonNode offering, string reason)
    {
        Assert.False((bool)offering["eligible"]!);
        Assert.Contains(reason, offering["reasons"]!.AsArray().Select(r => (string?)r));
    }
}
