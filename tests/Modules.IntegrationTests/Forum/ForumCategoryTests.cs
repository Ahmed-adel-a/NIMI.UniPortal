// ACCEPTANCE TEST — written by the consultant for phase P11. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Forum.ForumSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Forum;

/// <summary>P11 Forum — categories are university-wide or one faculty's; Student Affairs manages them.</summary>
public sealed class ForumCategoryTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task Members_see_university_wide_categories_and_their_own_facultys()
    {
        var f = await BuildForumAsync(Factory, Admin());

        var ahmed = await CategoriesAsync(f.Campus.AhmedClient);
        var sara = await CategoriesAsync(f.SaraClient);

        Assert.Equal(new[] { f.General, f.Engineering }.Order(), ahmed.Select(c => (Guid)c!["id"]!).Order());
        Assert.Equal([f.General], sara.Select(c => (Guid)c!["id"]!));
        var engineering = ahmed.Single(c => (Guid)c!["id"]! == f.Engineering)!;
        Assert.Equal("Engineering", (string?)engineering["name"]);
        Assert.Equal("Engineering (ar)", (string?)engineering["nameAr"]);
        Assert.Equal(f.Campus.FacultyId, (Guid)engineering["facultyId"]!);
        Assert.True((bool)engineering["isActive"]!);
    }

    [Fact]
    public async Task Categories_are_ordered_by_sort_order_then_name()
    {
        var f = await BuildForumAsync(Factory, Admin());
        var first = await CreatedCategoryAsync(Admin(), "Announcements board", null, sortOrder: -1);

        var listed = await CategoriesAsync(f.Campus.AhmedClient);

        Assert.Equal(first, (Guid)listed[0]!["id"]!);
    }

    [Fact]
    public async Task A_deactivated_category_disappears_for_members_but_not_for_student_affairs()
    {
        var f = await BuildForumAsync(Factory, Admin());

        var update = await Admin().PutAsJsonAsync($"/api/admin/forum/categories/{f.Engineering}",
            new { name = "Engineering", nameAr = "الهندسة", facultyId = f.Campus.FacultyId, sortOrder = 5, isActive = false });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.DoesNotContain(await CategoriesAsync(f.Campus.AhmedClient), c => (Guid)c!["id"]! == f.Engineering);
        Assert.Contains(await CategoriesAsync(Admin()), c => (Guid)c!["id"]! == f.Engineering);
        Assert.Equal(HttpStatusCode.NotFound, (await StartThreadAsync(f.Campus.AhmedClient, f.Engineering)).StatusCode);
    }

    [Fact]
    public async Task Categories_are_validated()
    {
        var noName = await CreateCategoryAsync(Admin(), " ", null);
        Assert.Equal(HttpStatusCode.BadRequest, noName.StatusCode);
        Assert.Contains("name", await FieldsOf(noName));

        var unknownFaculty = await CreateCategoryAsync(Admin(), "Ghost", Guid.CreateVersion7());
        Assert.Equal(HttpStatusCode.BadRequest, unknownFaculty.StatusCode);
        Assert.Contains("facultyId", await FieldsOf(unknownFaculty));

        Assert.Equal(HttpStatusCode.NotFound, (await Admin().PutAsJsonAsync($"/api/admin/forum/categories/{Guid.CreateVersion7()}",
            new { name = "X", nameAr = "X", facultyId = (Guid?)null, sortOrder = 0, isActive = true })).StatusCode);
    }

    [Fact]
    public async Task Only_student_affairs_manages_categories()
    {
        var f = await BuildForumAsync(Factory, Admin());

        Assert.Equal(HttpStatusCode.Forbidden, (await CreateCategoryAsync(f.Campus.HanyClient, "Mine", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await CreateCategoryAsync(f.Campus.AhmedClient, "Mine", null)).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private static async Task<JsonArray> CategoriesAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<JsonArray>("/api/forum/categories"))!;
}
