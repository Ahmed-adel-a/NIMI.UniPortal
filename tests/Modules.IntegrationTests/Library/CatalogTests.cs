// ACCEPTANCE TEST — written by the consultant for phase P11. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Library.LibrarySetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Library;

/// <summary>P11 Library — Student Affairs keeps the catalog (books, barcoded copies); members search it.</summary>
public sealed class CatalogTests(ReferenceHostFactory factory) : ReferenceHostTest(factory)
{
    [Fact]
    public async Task A_book_with_copies_is_found_by_title_author_or_isbn()
    {
        var w = await BuildAsync(Factory, Admin());

        var added = await AddBookAsync(Admin(), "Introduction to Algorithms", "0-262-04630-X");
        Assert.Equal(HttpStatusCode.Created, added.StatusCode);
        var book = await BodyOf(added);
        Assert.Equal("9780262046305", (string?)book["isbn"]);                     // ISBN-10 stored as ISBN-13
        var bookId = (Guid)book["id"]!;
        Assert.Equal(HttpStatusCode.Created, (await AddCopyAsync(Admin(), bookId, "LIB-0001")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await AddCopyAsync(Admin(), bookId, "LIB-0002")).StatusCode);
        var other = await AddBookAsync(Admin(), "Database System Concepts", "9780078022159", authors: "Silberschatz, Korth, Sudarshan");
        Assert.Equal(HttpStatusCode.Created, (await AddCopyAsync(Admin(), (Guid)(await BodyOf(other))["id"]!, "LIB-0100")).StatusCode);

        foreach (var search in new[] { "algorithms", "Leiserson", "9780262046305" })
        {
            var found = (await w.AhmedClient.GetFromJsonAsync<JsonObject>($"/api/library/books?search={search}"))!;
            var item = Assert.Single(found["items"]!.AsArray())!;
            Assert.Equal(bookId, (Guid)item["id"]!);
            Assert.Equal(2, (int)item["totalCopies"]!);
            Assert.Equal(2, (int)item["availableCopies"]!);
        }

        var detail = await BookAsync(w.AhmedClient, bookId);
        Assert.Equal(2, detail["copies"]!.AsArray().Count);
        Assert.Equal("available", (string?)detail["copies"]![0]!["status"]);
        Assert.Equal(0, (int)detail["waiting"]!);
    }

    [Fact]
    public async Task A_lost_copy_is_no_longer_available()
    {
        var w = await BuildAsync(Factory, Admin());
        var bookId = await BookWithCopiesAsync(Admin(), "Compilers", null, "LIB-0200");
        var copyId = (Guid)(await BookAsync(Admin(), bookId))["copies"]![0]!["id"]!;

        var lost = await Admin().PutAsJsonAsync($"/api/admin/library/copies/{copyId}", new { status = "lost", shelf = (string?)null });

        Assert.Equal(HttpStatusCode.OK, lost.StatusCode);
        var item = (await w.AhmedClient.GetFromJsonAsync<JsonObject>("/api/library/books?search=Compilers"))!["items"]![0]!;
        Assert.Equal(0, (int)item["availableCopies"]!);
        Assert.Equal(HttpStatusCode.Conflict, (await LendAsync(Admin(), "LIB-0200", w.Ahmed)).StatusCode);
    }

    [Fact]
    public async Task The_catalog_is_validated()
    {
        await BuildAsync(Factory, Admin());

        var badIsbn = await AddBookAsync(Admin(), "Bad", "978-0-00-000000-1");
        Assert.Equal(HttpStatusCode.BadRequest, badIsbn.StatusCode);
        Assert.Contains("isbn", await FieldsOf(badIsbn));

        var noTitle = await AddBookAsync(Admin(), " ", null);
        Assert.Equal(HttpStatusCode.BadRequest, noTitle.StatusCode);
        Assert.Contains("title", await FieldsOf(noTitle));

        var bookId = await BookWithCopiesAsync(Admin(), "Introduction to Algorithms", "9780262046305", "LIB-0001");
        Assert.Equal(HttpStatusCode.Conflict, (await AddBookAsync(Admin(), "Same ISBN", "9780262046305")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await AddCopyAsync(Admin(), bookId, "LIB-0001")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await AddCopyAsync(Admin(), Guid.CreateVersion7(), "LIB-0999")).StatusCode);
    }

    [Fact]
    public async Task Only_student_affairs_keeps_the_catalog()
    {
        var w = await BuildAsync(Factory, Admin());

        Assert.Equal(HttpStatusCode.Forbidden, (await AddBookAsync(w.HanyClient)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await AddBookAsync(w.AhmedClient)).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");
}
