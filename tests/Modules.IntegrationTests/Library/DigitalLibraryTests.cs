// ACCEPTANCE TEST — written by the consultant for the digital library (decision DIGILIB). Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Users;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Library;

/// <summary>
/// Decision DIGILIB: Student Affairs adds verified PDF/EPUB books for the university, a faculty or a department
/// (optionally a subject); members see and download only the books meant for them; downloads are counted.
/// </summary>
public sealed class DigitalLibraryTests(ReferenceHostFactory factory) : ReferenceHostTest(factory)
{
    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.4\n% a tiny test book\n%%EOF\n");

    [Fact]
    public async Task A_member_downloads_a_book_and_the_download_is_counted()
    {
        var admin = Factory.CreateClientAs("admin");
        var w = await LibrarySetup.BuildAsync(Factory, admin);
        var added = await AddAsync(admin, "Signals and Systems", "Oppenheim, Willsky", "signals.pdf", Pdf);
        Assert.Equal(HttpStatusCode.Created, added.StatusCode);
        var book = await BodyOf(added);
        Assert.Equal("pdf", (string?)book["format"]);

        var download = await w.AhmedClient.GetAsync($"/api/library/digital/{book["id"]}/download");

        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("application/pdf", download.Content.Headers.ContentType?.MediaType);
        Assert.Equal(Pdf, await download.Content.ReadAsByteArrayAsync());
        var listed = (await ListAsync(w.AhmedClient)).Single();
        Assert.Equal(1, (int)listed!["downloadCount"]!);
    }

    [Fact]
    public async Task Each_member_sees_only_the_books_for_their_university_faculty_and_department()
    {
        var admin = Factory.CreateClientAs("admin");
        var w = await LibrarySetup.BuildAsync(Factory, admin);
        var ee = await OrganizationSetup.CreateDepartmentAsync(admin, w.Eng, "EE");
        var med = await OrganizationSetup.CreateFacultyAsync(admin, "MED");
        var surgery = await OrganizationSetup.CreateDepartmentAsync(admin, med, "SUR");
        var (_, saraToken) = await AccountSetup.CreateStudentWithTokenAsync(Factory, admin, ee, "sara@asu.edu.eg");
        var (_, omarToken) = await AccountSetup.CreateStudentWithTokenAsync(Factory, admin, surgery, "omar@asu.edu.eg");

        await AddAsync(admin, "University handbook", "Student Affairs", "handbook.pdf", Pdf);
        await AddAsync(admin, "Engineering mathematics", "Stroud", "math.pdf", Pdf, facultyId: w.Eng);
        var cs = await BodyOf(await AddAsync(admin, "Clean Code", "Martin", "clean-code.epub", Encoding.ASCII.GetBytes("PK epub"), departmentId: w.Cs));
        await AddAsync(admin, "Gray's Anatomy", "Gray", "anatomy.pdf", Pdf, facultyId: med);

        Assert.Equal(3, (await ListAsync(w.AhmedClient)).Count);
        Assert.Equal(2, (await ListAsync(Factory.CreateClientWithToken(saraToken))).Count);
        var omar = Factory.CreateClientWithToken(omarToken);
        Assert.Equal(["Gray's Anatomy", "University handbook"], (await ListAsync(omar)).Select(b => (string)b!["title"]!).Order().ToArray());
        Assert.Equal(HttpStatusCode.NotFound, (await omar.GetAsync($"/api/library/digital/{cs["id"]}/download")).StatusCode);
        Assert.Equal(4, (await ListAsync(admin)).Count);
        Assert.Equal("epub", (string?)cs["format"]);
        Assert.Equal(w.Eng, (Guid)cs["facultyId"]!);   // a department implies its faculty
    }

    [Fact]
    public async Task Only_student_affairs_add_books_and_only_pdf_or_epub()
    {
        var admin = Factory.CreateClientAs("admin");
        var w = await LibrarySetup.BuildAsync(Factory, admin);
        var med = await OrganizationSetup.CreateFacultyAsync(admin, "MED");

        var docx = await AddAsync(admin, "Notes", "Someone", "notes.docx", Pdf);
        var otherFaculty = await AddAsync(admin, "Clean Code", "Martin", "clean.pdf", Pdf, facultyId: med, departmentId: w.Cs);
        var noTitle = await AddAsync(admin, "", "Martin", "clean.pdf", Pdf);

        Assert.Equal(HttpStatusCode.BadRequest, docx.StatusCode);
        Assert.Contains("file", await FieldsOf(docx));
        Assert.Equal(HttpStatusCode.BadRequest, otherFaculty.StatusCode);
        Assert.Contains("departmentId", await FieldsOf(otherFaculty));
        Assert.Equal(HttpStatusCode.BadRequest, noTitle.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await AddAsync(w.HanyClient, "Mine", "Hany", "mine.pdf", Pdf)).StatusCode);
    }

    [Fact]
    public async Task Student_affairs_edits_and_removes_books()
    {
        var admin = Factory.CreateClientAs("admin");
        var w = await LibrarySetup.BuildAsync(Factory, admin);
        var book = await BodyOf(await AddAsync(admin, "Draft title", "Author", "book.pdf", Pdf));

        var edited = await admin.PutAsJsonAsync($"/api/admin/library/digital/{book["id"]}",
            new { title = "Final title", authors = "Author", description = "Second edition", facultyId = w.Eng });
        Assert.Equal(HttpStatusCode.OK, edited.StatusCode);
        Assert.Equal("Final title", (string?)(await ListAsync(w.AhmedClient)).Single()!["title"]);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/admin/library/digital/{book["id"]}")).StatusCode);
        Assert.Empty(await ListAsync(w.AhmedClient));
        Assert.Equal(HttpStatusCode.NotFound, (await w.AhmedClient.GetAsync($"/api/library/digital/{book["id"]}/download")).StatusCode);
    }

    [Fact]
    public async Task Books_can_be_searched_and_filtered_by_subject()
    {
        var admin = Factory.CreateClientAs("admin");
        var w = await LibrarySetup.BuildAsync(Factory, admin);
        var algorithms = await CreateSubjectAsync(admin, w.Cs, "CS301");
        await AddAsync(admin, "Introduction to Algorithms", "Cormen", "clrs.pdf", Pdf, subjectId: algorithms);
        await AddAsync(admin, "Database Systems", "Elmasri", "db.pdf", Pdf);

        Assert.Single(await ListAsync(w.AhmedClient, "?search=cormen"));
        var bySubject = Assert.Single(await ListAsync(w.AhmedClient, $"?subjectId={algorithms}"));
        Assert.Equal(algorithms, (Guid)bySubject!["subjectId"]!);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await AddAsync(admin, "X", "Y", "x.pdf", Pdf, subjectId: Guid.CreateVersion7())).StatusCode);
    }

    private static async Task<HttpResponseMessage> AddAsync(
        HttpClient client, string title, string authors, string fileName, byte[] bytes,
        Guid? facultyId = null, Guid? departmentId = null, Guid? subjectId = null)
    {
        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(file, "file", fileName);
        content.Add(new StringContent(title), "title");
        content.Add(new StringContent(authors), "authors");
        if (facultyId is not null)
            content.Add(new StringContent(facultyId.ToString()!), "facultyId");
        if (departmentId is not null)
            content.Add(new StringContent(departmentId.ToString()!), "departmentId");
        if (subjectId is not null)
            content.Add(new StringContent(subjectId.ToString()!), "subjectId");
        return await client.PostAsync("/api/admin/library/digital", content);
    }

    private static async Task<JsonArray> ListAsync(HttpClient client, string query = "") =>
        (await client.GetFromJsonAsync<JsonObject>("/api/library/digital" + query))!["items"]!.AsArray();
}
