// ACCEPTANCE TEST — written by the consultant for phase P03. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Users;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Scheduling;

/// <summary>P3.4 — halls, labs and other facilities, per faculty or shared by the university.</summary>
public sealed class FacilityTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task Admin_creates_a_facility()
    {
        var eng = await OrganizationSetup.CreateFacultyAsync(Admin(), "ENG");

        var response = await PostFacilityAsync(Admin(), "مدرج ١", eng, "auditorium", 400);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var hall = await BodyOf(response);
        Assert.Equal(eng, (Guid)hall["facultyId"]!);
        Assert.Equal("مدرج ١", (string?)hall["name"]);
        Assert.Equal("auditorium", (string?)hall["type"]);
        Assert.Equal(400, (int)hall["capacity"]!);
        Assert.Equal("Main building", (string?)hall["building"]);
        Assert.True((bool)hall["isActive"]!);
    }

    [Fact]
    public async Task Filtering_by_faculty_returns_its_halls_and_the_shared_ones()
    {
        var eng = await OrganizationSetup.CreateFacultyAsync(Admin(), "ENG");
        var com = await OrganizationSetup.CreateFacultyAsync(Admin(), "COM");
        await CreateFacilityAsync(Admin(), "ENG Hall", eng);
        await CreateFacilityAsync(Admin(), "COM Hall", com);
        await CreateFacilityAsync(Admin(), "Central Auditorium");

        var forEngineering = (await Student().GetFromJsonAsync<JsonObject>($"/api/facilities?facultyId={eng}"))!;
        var all = (await Student().GetFromJsonAsync<JsonObject>("/api/facilities"))!;

        Assert.Equal(["Central Auditorium", "ENG Hall"], Names(forEngineering));    // ordered by name
        Assert.Equal(3, (int)all["totalCount"]!);
    }

    [Fact]
    public async Task A_name_is_unique_within_a_faculty_only()
    {
        var eng = await OrganizationSetup.CreateFacultyAsync(Admin(), "ENG");
        var com = await OrganizationSetup.CreateFacultyAsync(Admin(), "COM");
        await CreateFacilityAsync(Admin(), "Hall 1", eng);

        var sameFaculty = await PostFacilityAsync(Admin(), "Hall 1", eng);
        var otherFaculty = await PostFacilityAsync(Admin(), "Hall 1", com);

        Assert.Equal(HttpStatusCode.Conflict, sameFaculty.StatusCode);
        Assert.Equal(HttpStatusCode.Created, otherFaculty.StatusCode);
    }

    [Fact]
    public async Task Invalid_facilities_are_a_400()
    {
        var zeroCapacity = await PostFacilityAsync(Admin(), "Hall 1", capacity: 0);
        var unknownFaculty = await PostFacilityAsync(Admin(), "Hall 2", Guid.CreateVersion7());
        var unknownType = await PostFacilityAsync(Admin(), "Hall 3", type: "swimming_pool");

        Assert.Equal(HttpStatusCode.BadRequest, zeroCapacity.StatusCode);
        Assert.Contains("capacity", await FieldsOf(zeroCapacity));
        Assert.Equal(HttpStatusCode.BadRequest, unknownFaculty.StatusCode);
        Assert.Contains("facultyId", await FieldsOf(unknownFaculty));
        Assert.Equal(HttpStatusCode.BadRequest, unknownType.StatusCode);
    }

    [Fact]
    public async Task A_deactivated_facility_is_hidden_unless_asked_for()
    {
        var id = await CreateFacilityAsync(Admin(), "Old Lab");

        var update = await Admin().PutAsJsonAsync($"/api/admin/facilities/{id}", new
        {
            facultyId = (Guid?)null, name = "Old Lab", type = "lab", capacity = 30, building = (string?)null, floor = (string?)null, isActive = false,
        });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("lab", (string?)(await BodyOf(update))["type"]);
        Assert.Empty(Names((await Student().GetFromJsonAsync<JsonObject>("/api/facilities"))!));
        Assert.Equal(["Old Lab"], Names((await Student().GetFromJsonAsync<JsonObject>("/api/facilities?includeInactive=true"))!));
    }

    [Fact]
    public async Task Only_admins_manage_facilities()
    {
        var body = new { name = "Hall 1", type = "lecture_hall", capacity = 100 };

        Assert.Equal(HttpStatusCode.Forbidden, (await Factory.CreateClientAs("professor").PostAsJsonAsync("/api/admin/facilities", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Factory.CreateClient().GetAsync("/api/facilities")).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private HttpClient Student() => Factory.CreateClientAs("student");

    private static List<string?> Names(JsonObject page) => page["items"]!.AsArray().Select(f => (string?)f!["name"]).ToList();
}
