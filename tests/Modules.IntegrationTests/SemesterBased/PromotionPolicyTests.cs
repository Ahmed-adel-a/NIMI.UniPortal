// ACCEPTANCE TEST — written by the consultant for phase P4.B. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Users;
using static NIMI.UniPortal.Modules.IntegrationTests.SemesterBased.SemesterSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.SemesterBased;

/// <summary>
/// Decision SEMPROMO: the promotion numbers are settings Student Affairs can change, university-wide and per faculty.
/// Built in: 3 carried subjects, no stage gates, 2 years per level, fees must be paid.
/// </summary>
public sealed class PromotionPolicyTests(ReferenceHostFactory factory) : ReferenceHostTest(factory)
{
    [Fact]
    public async Task Without_settings_the_built_in_defaults_apply()
    {
        var policy = (await Factory.CreateClientAs("admin").GetFromJsonAsync<JsonObject>("/api/admin/promotion-policy"))!;

        Assert.Equal("default", (string?)policy["source"]);
        Assert.Equal(3, (int)policy["maxCarriedSubjects"]!);
        Assert.Empty(policy["gateLevels"]!.AsArray());
        Assert.Equal(2, (int)policy["maxYearsPerLevel"]!);
        Assert.True((bool)policy["requireFeesPaid"]!);
    }

    [Fact]
    public async Task A_faculty_override_wins_until_it_is_removed()
    {
        var admin = Factory.CreateClientAs("admin");
        var eng = await OrganizationSetup.CreateFacultyAsync(admin, "ENG");

        Assert.Equal(HttpStatusCode.OK, (await SavePolicyAsync(admin, maxCarriedSubjects: 2)).StatusCode);
        var inherited = (await admin.GetFromJsonAsync<JsonObject>($"/api/admin/faculties/{eng}/promotion-policy"))!;
        Assert.Equal("university", (string?)inherited["source"]);
        Assert.Equal(2, (int)inherited["maxCarriedSubjects"]!);

        var saved = await SavePolicyAsync(admin, maxCarriedSubjects: 0, gateLevels: [3], facultyId: eng);
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var own = (await admin.GetFromJsonAsync<JsonObject>($"/api/admin/faculties/{eng}/promotion-policy"))!;
        Assert.Equal("faculty", (string?)own["source"]);
        Assert.Equal(0, (int)own["maxCarriedSubjects"]!);
        Assert.Equal([3], own["gateLevels"]!.AsArray().Select(g => (int)g!).ToArray());

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/admin/faculties/{eng}/promotion-policy")).StatusCode);
        var back = (await admin.GetFromJsonAsync<JsonObject>($"/api/admin/faculties/{eng}/promotion-policy"))!;
        Assert.Equal("university", (string?)back["source"]);
    }

    [Fact]
    public async Task Out_of_range_settings_are_rejected()
    {
        var admin = Factory.CreateClientAs("admin");

        Assert.Equal(HttpStatusCode.BadRequest, (await SavePolicyAsync(admin, maxCarriedSubjects: 11)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await SavePolicyAsync(admin, gateLevels: [1])).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await SavePolicyAsync(admin, maxYearsPerLevel: 0)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await SavePolicyAsync(admin, facultyId: Guid.CreateVersion7())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SavePolicyAsync(Factory.CreateClientAs("student"))).StatusCode);
    }
}
