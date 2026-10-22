// ACCEPTANCE TEST — written by the consultant for phase P11. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Library.LibrarySetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Library;

/// <summary>P11 Library — Student Affairs lends and takes back copies under each role's loan rules.</summary>
public sealed class LoanTests(ReferenceHostFactory factory) : ReferenceHostTest(factory)
{
    [Fact]
    public async Task A_student_borrows_for_14_days_and_sees_the_loan()
    {
        var w = await BuildAsync(Factory, Admin());
        var bookId = await BookWithCopiesAsync(Admin(), "Introduction to Algorithms", null, "LIB-0001");
        var now = Factory.Clock.GetUtcNow();

        var loan = await LentAsync(Admin(), "LIB-0001", w.Ahmed);

        Assert.Equal(bookId, (Guid)loan["bookId"]!);
        Assert.Equal("LIB-0001", (string?)loan["barcode"]);
        Assert.Equal(w.Ahmed, (Guid)loan["userId"]!);
        Assert.InRange(((DateTimeOffset)loan["dueAt"]! - now).TotalDays, 13.99, 14.01);
        Assert.False((bool)loan["isOverdue"]!);
        Assert.Equal("on_loan", (string?)(await BookAsync(w.AhmedClient, bookId))["copies"]![0]!["status"]);
        var mine = (await w.AhmedClient.GetFromJsonAsync<JsonObject>("/api/library/loans/mine"))!;
        Assert.Equal((Guid)loan["id"]!, (Guid)mine["items"]![0]!["id"]!);
    }

    [Fact]
    public async Task A_professor_borrows_for_30_days()
    {
        var w = await BuildAsync(Factory, Admin());
        await BookWithCopiesAsync(Admin(), "Compilers", null, "LIB-0001");
        var now = Factory.Clock.GetUtcNow();

        var loan = await LentAsync(Admin(), "LIB-0001", w.Hany);

        Assert.InRange(((DateTimeOffset)loan["dueAt"]! - now).TotalDays, 29.99, 30.01);
    }

    [Fact]
    public async Task Only_available_copies_lend_and_only_to_members()
    {
        var w = await BuildAsync(Factory, Admin());
        await BookWithCopiesAsync(Admin(), "Compilers", null, "LIB-0001");
        await LentAsync(Admin(), "LIB-0001", w.Ahmed);

        Assert.Equal(HttpStatusCode.Conflict, (await LendAsync(Admin(), "LIB-0001", w.Mona)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await LendAsync(Admin(), "LIB-9999", w.Mona)).StatusCode);
        var notMember = await LendAsync(Admin(), "LIB-0001", Guid.CreateVersion7());
        Assert.Equal(HttpStatusCode.BadRequest, notMember.StatusCode);
        Assert.Contains("userId", await FieldsOf(notMember));
    }

    [Fact]
    public async Task A_student_holds_at_most_3_books()
    {
        var w = await BuildAsync(Factory, Admin());
        await BookWithCopiesAsync(Admin(), "Book A", null, "A1");
        await BookWithCopiesAsync(Admin(), "Book B", null, "B1");
        await BookWithCopiesAsync(Admin(), "Book C", null, "C1");
        await BookWithCopiesAsync(Admin(), "Book D", null, "D1");
        foreach (var barcode in new[] { "A1", "B1", "C1" })
            await LentAsync(Admin(), barcode, w.Ahmed);

        var fourth = await LendAsync(Admin(), "D1", w.Ahmed);

        Assert.Equal(HttpStatusCode.Conflict, fourth.StatusCode);
        Assert.Contains("3", (string?)(await BodyOf(fourth))["message"]);
    }

    [Fact]
    public async Task An_overdue_loan_blocks_new_ones_and_shows_on_the_overdue_list()
    {
        var w = await BuildAsync(Factory, Admin());
        await BookWithCopiesAsync(Admin(), "Book A", null, "A1");
        await BookWithCopiesAsync(Admin(), "Book B", null, "B1");
        var loan = await LentAsync(Admin(), "A1", w.Ahmed);

        Factory.Clock.Advance(TimeSpan.FromDays(15));

        Assert.Equal(HttpStatusCode.Conflict, (await LendAsync(Admin(), "B1", w.Ahmed)).StatusCode);
        var overdue = (await Admin().GetFromJsonAsync<JsonObject>("/api/admin/library/loans?overdue=true"))!;
        var item = Assert.Single(overdue["items"]!.AsArray())!;
        Assert.Equal((Guid)loan["id"]!, (Guid)item["id"]!);
        Assert.True((bool)item["isOverdue"]!);
    }

    [Fact]
    public async Task A_return_frees_the_copy_once()
    {
        var w = await BuildAsync(Factory, Admin());
        var bookId = await BookWithCopiesAsync(Admin(), "Compilers", null, "LIB-0001");
        var loanId = (Guid)(await LentAsync(Admin(), "LIB-0001", w.Ahmed))["id"]!;

        var returned = await ReturnAsync(Admin(), loanId);

        Assert.Equal(HttpStatusCode.OK, returned.StatusCode);
        Assert.NotNull((await BodyOf(returned))["returnedAt"]);
        Assert.Equal("available", (string?)(await BookAsync(w.MonaClient, bookId))["copies"]![0]!["status"]);
        Assert.Equal(HttpStatusCode.Conflict, (await ReturnAsync(Admin(), loanId)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await LendAsync(Admin(), "LIB-0001", w.Mona)).StatusCode);
    }

    [Fact]
    public async Task Only_student_affairs_lends()
    {
        var w = await BuildAsync(Factory, Admin());
        await BookWithCopiesAsync(Admin(), "Compilers", null, "LIB-0001");

        Assert.Equal(HttpStatusCode.Forbidden, (await LendAsync(w.HanyClient, "LIB-0001", w.Ahmed)).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");
}
