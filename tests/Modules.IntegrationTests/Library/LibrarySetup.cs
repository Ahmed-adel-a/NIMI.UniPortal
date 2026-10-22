// ACCEPTANCE TEST — written by the consultant for phase P11. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Users;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Library;

/// <summary>Library helpers through the reference host's API (P11: LIBKIND physical lending, LIBROLE Student Affairs).</summary>
internal static class LibrarySetup
{
    /// <summary>ENG faculty, CS department: students Ahmed and Mona, professor Hany.</summary>
    public static async Task<LibraryWorld> BuildAsync(ReferenceHostFactory factory, HttpClient admin)
    {
        var eng = await OrganizationSetup.CreateFacultyAsync(admin, "ENG");
        var cs = await OrganizationSetup.CreateDepartmentAsync(admin, eng, "CS");
        var (ahmed, ahmedToken) = await AccountSetup.CreateStudentWithTokenAsync(factory, admin, cs, "ahmed@asu.edu.eg");
        var (mona, monaToken) = await AccountSetup.CreateStudentWithTokenAsync(factory, admin, cs, "mona@asu.edu.eg");
        var (hany, hanyToken) = await AccountSetup.CreateProfessorWithTokenAsync(factory, admin, cs, "hany@asu.edu.eg");
        return new LibraryWorld(
            eng, cs,
            ahmed, factory.CreateClientWithToken(ahmedToken),
            mona, factory.CreateClientWithToken(monaToken),
            hany, factory.CreateClientWithToken(hanyToken));
    }

    public static async Task<HttpResponseMessage> AddBookAsync(
        HttpClient admin, string title = "Introduction to Algorithms", string? isbn = "9780262046305",
        string authors = "Cormen, Leiserson, Rivest, Stein", Guid? facultyId = null) =>
        await admin.PostAsJsonAsync("/api/admin/library/books", new
        {
            isbn, title, authors, publisher = "MIT Press", publicationYear = 2022, facultyId,
        });

    /// <summary>Adds a book with the given copies (barcodes); returns the book id.</summary>
    public static async Task<Guid> BookWithCopiesAsync(
        HttpClient admin, string title, string? isbn, params string[] barcodes)
    {
        var added = await AddBookAsync(admin, title, isbn);
        Assert.Equal(HttpStatusCode.Created, added.StatusCode);
        var bookId = (Guid)(await BodyOf(added))["id"]!;
        foreach (var barcode in barcodes)
            Assert.Equal(HttpStatusCode.Created, (await AddCopyAsync(admin, bookId, barcode)).StatusCode);
        return bookId;
    }

    public static async Task<HttpResponseMessage> AddCopyAsync(HttpClient admin, Guid bookId, string barcode, string? shelf = "QA76 C67") =>
        await admin.PostAsJsonAsync($"/api/admin/library/books/{bookId}/copies", new { barcode, shelf });

    public static async Task<HttpResponseMessage> LendAsync(HttpClient admin, string barcode, Guid userId) =>
        await admin.PostAsJsonAsync("/api/admin/library/loans", new { barcode, userId });

    public static async Task<JsonObject> LentAsync(HttpClient admin, string barcode, Guid userId)
    {
        var response = await LendAsync(admin, barcode, userId);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await BodyOf(response);
    }

    public static async Task<HttpResponseMessage> ReturnAsync(HttpClient admin, Guid loanId) =>
        await admin.PostAsync($"/api/admin/library/loans/{loanId}/return", null);

    public static async Task<HttpResponseMessage> ReserveAsync(HttpClient member, Guid bookId) =>
        await member.PostAsync($"/api/library/books/{bookId}/reservations", null);

    public static async Task<JsonObject> BookAsync(HttpClient client, Guid bookId) =>
        (await client.GetFromJsonAsync<JsonObject>($"/api/library/books/{bookId}"))!;

    public static async Task<JsonObject> RunRemindersAsync(HttpClient admin)
    {
        var response = await admin.PostAsync("/api/admin/library/reminders/run", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await BodyOf(response);
    }

    public static async Task<int> UnreadAsync(HttpClient client) =>
        (int)(await client.GetFromJsonAsync<JsonObject>("/api/notifications/mine/unread-count"))!["count"]!;
}
