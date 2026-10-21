// ACCEPTANCE TEST — written by the consultant for phase P05-payments. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Payments.PaymentsSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Payments;

/// <summary>P5.1 — fee items defined by Student Affairs (PAYOWN), targeted at the university, a faculty or a department.</summary>
public sealed class FeeItemTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task Admin_defines_a_fee_item()
    {
        var w = await World();

        var response = await PostFeeItemAsync(Admin(), "Books", "other", 250.75m, w.TermId, w.Eng, w.Cs, "2026-11-30", "الكتب");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var fee = await BodyOf(response);
        Assert.Equal("Books", (string?)fee["name"]);
        Assert.Equal("الكتب", (string?)fee["nameAr"]);
        Assert.Equal("other", (string?)fee["category"]);
        Assert.Equal(250.75m, (decimal)fee["amount"]!);
        Assert.Equal(w.TermId, (Guid)fee["termId"]!);
        Assert.Equal(w.Eng, (Guid)fee["facultyId"]!);
        Assert.Equal(w.Cs, (Guid)fee["departmentId"]!);
        Assert.Equal("2026-11-30", (string?)fee["dueOn"]);
        Assert.True((bool)fee["isActive"]!);
    }

    [Fact]
    public async Task Fee_items_are_listed_and_filterable_by_category()
    {
        var w = await World();

        var all = (await Admin().GetFromJsonAsync<JsonObject>("/api/admin/fee-items"))!;
        var activities = (await Admin().GetFromJsonAsync<JsonObject>("/api/admin/fee-items?category=activity"))!;

        Assert.Equal(4, (int)all["totalCount"]!);
        Assert.Equal(["CS club"], activities["items"]!.AsArray().Select(f => (string?)f!["name"]));
    }

    [Fact]
    public async Task Invalid_fee_items_are_a_400_naming_the_field()
    {
        var w = await World();

        var zero = await PostFeeItemAsync(Admin(), "Zero", "other", 0m);
        var unknownTerm = await PostFeeItemAsync(Admin(), "X", "other", 10m, termId: Guid.CreateVersion7());
        var unknownFaculty = await PostFeeItemAsync(Admin(), "X", "other", 10m, facultyId: Guid.CreateVersion7());
        var wrongFaculty = await PostFeeItemAsync(Admin(), "X", "other", 10m, facultyId: w.Com, departmentId: w.Cs);

        Assert.Contains("amount", await FieldsOf(zero));
        Assert.Contains("termId", await FieldsOf(unknownTerm));
        Assert.Contains("facultyId", await FieldsOf(unknownFaculty));
        Assert.Contains("departmentId", await FieldsOf(wrongFaculty));
        Assert.All(new[] { zero, unknownTerm, unknownFaculty, wrongFaculty }, r => Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode));
    }

    [Fact]
    public async Task Fee_items_are_deactivated_never_deleted_and_hidden_unless_asked_for()
    {
        var w = await World();

        var update = await Admin().PutAsJsonAsync($"/api/admin/fee-items/{w.CsClub}", new
        {
            name = "CS club", nameAr = "نادي الحاسبات", description = (string?)null, amount = 120m, dueOn = "2099-12-31", isActive = false,
        });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.False((bool)(await BodyOf(update))["isActive"]!);
        Assert.Equal(120m, (decimal)(await BodyOf(update))["amount"]!);
        Assert.Equal(3, (int)(await Admin().GetFromJsonAsync<JsonObject>("/api/admin/fee-items"))!["totalCount"]!);
        Assert.Equal(4, (int)(await Admin().GetFromJsonAsync<JsonObject>("/api/admin/fee-items?includeInactive=true"))!["totalCount"]!);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await Admin().DeleteAsync($"/api/admin/fee-items/{w.CsClub}")).StatusCode);
    }

    [Fact]
    public async Task Only_admins_manage_fee_items()
    {
        var w = await World();

        Assert.Equal(HttpStatusCode.Forbidden, (await w.AhmedClient.GetAsync("/api/admin/fee-items")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await PostFeeItemAsync(Factory.CreateClientAs("professor"), "X", "other", 10m)).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private Task<PaymentsWorld> World() => BuildAsync(Factory, Admin());
}
