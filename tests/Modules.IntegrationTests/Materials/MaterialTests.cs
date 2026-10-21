// ACCEPTANCE TEST — written by the consultant for phase P07-materials. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Lifecycle;
using static NIMI.UniPortal.Modules.IntegrationTests.Lifecycle.RegistrationSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Materials.MaterialsSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Materials;

/// <summary>
/// P7.0–P7.3 — course files (D1, R1–R5). Campus: <see cref="CampusWorld"/> — Hany teaches CS301 (both groups); in these
/// tests Ahmed is registered in CS301 group 1 and Mona in CS301 group 2.
/// </summary>
public sealed class MaterialTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task The_teaching_professor_uploads_a_pdf()
    {
        var w = await Campus();

        var response = await UploadAsync(w.HanyClient, w.Cs301, weekNumber: 3, description: "شرح الرسوم البيانية");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var material = await BodyOf(response);
        Assert.Equal(w.Cs301, (Guid)material["subjectId"]!);
        Assert.Equal("CS301", (string?)material["subjectCode"]);
        Assert.Null(material["subjectGroupId"]);
        Assert.Equal("Lecture 3 — Graphs", (string?)material["title"]);
        Assert.Equal("شرح الرسوم البيانية", (string?)material["description"]);
        Assert.Equal(3, (int)material["weekNumber"]!);
        Assert.Equal("graphs.pdf", (string?)material["fileName"]);
        Assert.Equal("application/pdf", (string?)material["contentType"]);
        Assert.Equal(PdfBytes.Length, (long)material["sizeBytes"]!);
        Assert.Equal("pdf", (string?)material["fileType"]);
        Assert.Equal(0, (int)material["downloadCount"]!);
        Assert.Equal(w.Hany, (Guid)material["uploadedBy"]!);
    }

    [Fact]
    public async Task Disallowed_or_missing_files_are_a_400_on_file()
    {
        var w = await Campus();

        var exe = await UploadAsync(w.HanyClient, w.Cs301, "tool.exe", contentType: "application/octet-stream");
        var empty = await UploadAsync(w.HanyClient, w.Cs301, "empty.pdf", content: []);

        Assert.Equal(HttpStatusCode.BadRequest, exe.StatusCode);
        Assert.Contains("file", await FieldsOf(exe));
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
        Assert.Contains("file", await FieldsOf(empty));
    }

    [Fact]
    public async Task Only_staff_teaching_the_subject_upload()
    {
        var w = await Campus();
        var (_, otherToken) = await AccountSetup.CreateProfessorWithTokenAsync(Factory, Admin(), w.CsDepartmentId, "laila@cu.edu.eg");

        Assert.Equal(HttpStatusCode.Forbidden, (await UploadAsync(Factory.CreateClientWithToken(otherToken), w.Cs301)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await UploadAsync(w.HanyClient, w.Cs302)).StatusCode);       // Hany doesn't teach CS302
        Assert.Equal(HttpStatusCode.Forbidden, (await UploadAsync(w.AhmedClient, w.Cs301)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await UploadAsync(Factory.CreateClient(), w.Cs301)).StatusCode);
    }

    [Fact]
    public async Task A_group_of_another_subject_is_a_400()
    {
        var w = await Campus();

        var response = await UploadAsync(w.HanyClient, w.Cs301, subjectGroupId: w.Cs302Group1);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("subjectGroupId", await FieldsOf(response));
    }

    [Fact]
    public async Task A_registered_student_lists_and_downloads_the_original_file()
    {
        var w = await Campus();
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);
        var id = await UploadedAsync(w.HanyClient, w.Cs301, "graphs.pdf", weekNumber: 3);

        var list = await ListAsync(w.AhmedClient, w.Cs301);
        var download = await w.AhmedClient.GetAsync($"/api/materials/{id}/download");

        Assert.Equal(1, (int)list["totalCount"]!);
        Assert.Equal(id, (Guid)list["items"]![0]!["id"]!);
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal(PdfBytes, await download.Content.ReadAsByteArrayAsync());
        Assert.Equal("application/pdf", download.Content.Headers.ContentType?.MediaType);
        Assert.Equal("graphs.pdf", (download.Content.Headers.ContentDisposition?.FileNameStar ?? download.Content.Headers.ContentDisposition?.FileName)?.Trim('"'));
        Assert.Equal(1, (int)(await ListAsync(w.AhmedClient, w.Cs301))["items"]![0]!["downloadCount"]!);
    }

    [Fact]
    public async Task Students_not_registered_in_the_subject_see_nothing()
    {
        var w = await Campus();
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);
        var id = await UploadedAsync(w.HanyClient, w.Cs301);

        Assert.Equal(HttpStatusCode.Forbidden, (await w.MonaClient.GetAsync($"/api/materials?subjectId={w.Cs301}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await w.MonaClient.GetAsync($"/api/materials/{id}/download")).StatusCode);
    }

    [Fact]
    public async Task Group_targeted_materials_reach_only_that_group()
    {
        var w = await Campus();
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);
        await RegisteredAsync(w.MonaClient, w.Cs301Group2);
        var forGroupTwo = await UploadedAsync(w.HanyClient, w.Cs301, "lab-sheet.docx", subjectGroupId: w.Cs301Group2, title: "Lab sheet");
        var forEveryone = await UploadedAsync(w.HanyClient, w.Cs301, "graphs.pdf");

        var ahmed = await ListAsync(w.AhmedClient, w.Cs301);
        var mona = await ListAsync(w.MonaClient, w.Cs301);

        Assert.Equal([forEveryone], Ids(ahmed));
        Assert.Equal(new[] { forEveryone, forGroupTwo }.Order(), Ids(mona).Order());
        Assert.Equal(HttpStatusCode.NotFound, (await w.AhmedClient.GetAsync($"/api/materials/{forGroupTwo}/download")).StatusCode);
    }

    [Fact]
    public async Task Materials_can_be_filtered_by_week_and_type_and_admins_see_them_all()
    {
        var w = await Campus();
        await UploadedAsync(w.HanyClient, w.Cs301, "week1.pdf", weekNumber: 1, title: "Week 1");
        await UploadedAsync(w.HanyClient, w.Cs301, "week2.pptx", weekNumber: 2, title: "Week 2");
        await UploadedAsync(w.HanyClient, w.Cs301, "extra.zip", title: "Extras");

        var weekTwo = await ListAsync(w.HanyClient, w.Cs301, "&weekNumber=2");
        var slides = await ListAsync(w.HanyClient, w.Cs301, "&type=ppt");
        var all = await ListAsync(Admin(), w.Cs301);

        Assert.Equal(["Week 2"], Titles(weekTwo));
        Assert.Equal(["Week 2"], Titles(slides));
        Assert.Equal(["Week 1", "Week 2", "Extras"], Titles(all));                    // by week, undated last
    }

    [Fact]
    public async Task The_subject_is_required_to_list_materials()
    {
        var w = await Campus();

        Assert.Equal(HttpStatusCode.BadRequest, (await w.HanyClient.GetAsync("/api/materials")).StatusCode);
    }

    [Fact]
    public async Task Teachers_edit_metadata_and_soft_delete_hides_the_material()
    {
        var w = await Campus();
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);
        var id = await UploadedAsync(w.HanyClient, w.Cs301, weekNumber: 3);

        var edit = await w.HanyClient.PutAsJsonAsync($"/api/materials/{id}", new { title = "Graphs (updated)", description = "With exercises", weekNumber = 4 });
        var studentEdit = await w.AhmedClient.PutAsJsonAsync($"/api/materials/{id}", new { title = "Hacked", description = (string?)null, weekNumber = 4 });
        var delete = await w.HanyClient.DeleteAsync($"/api/materials/{id}");

        Assert.Equal(HttpStatusCode.OK, edit.StatusCode);
        Assert.Equal("Graphs (updated)", (string?)(await BodyOf(edit))["title"]);
        Assert.Equal(4, (int)(await BodyOf(edit))["weekNumber"]!);
        Assert.Equal(HttpStatusCode.Forbidden, studentEdit.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(0, (int)(await ListAsync(w.AhmedClient, w.Cs301))["totalCount"]!);
        Assert.Equal(HttpStatusCode.NotFound, (await w.AhmedClient.GetAsync($"/api/materials/{id}/download")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await w.HanyClient.DeleteAsync($"/api/materials/{id}")).StatusCode);
    }

    [Fact]
    public async Task A_new_material_notifies_the_registered_students()
    {
        var w = await Campus();
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);
        await RegisteredAsync(w.MonaClient, w.Cs301Group2);

        await UploadedAsync(w.HanyClient, w.Cs301, subjectGroupId: w.Cs301Group1);

        Assert.Equal(1, await UnreadAsync(w.AhmedClient));
        Assert.Equal(0, await UnreadAsync(w.MonaClient));                              // other group
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private Task<CampusWorld> Campus() => BuildCampusAsync(Factory, Admin());

    private static async Task<JsonObject> ListAsync(HttpClient client, Guid subjectId, string extra = "") =>
        (await client.GetFromJsonAsync<JsonObject>($"/api/materials?subjectId={subjectId}{extra}"))!;

    private static List<Guid> Ids(JsonObject page) => page["items"]!.AsArray().Select(m => (Guid)m!["id"]!).ToList();

    private static List<string?> Titles(JsonObject page) => page["items"]!.AsArray().Select(m => (string?)m!["title"]).ToList();

    private static async Task<int> UnreadAsync(HttpClient client) =>
        (int)(await client.GetFromJsonAsync<JsonObject>("/api/notifications/mine/unread-count"))!["count"]!;
}
