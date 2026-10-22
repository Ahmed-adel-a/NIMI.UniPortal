// ACCEPTANCE TEST — written by the consultant for phase P11. Do not modify; report problems instead.
using System.Net;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Library.LibrarySetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Library;

/// <summary>P11 Library — members renew their own loans before the due date, within the role's limit, if nobody waits.</summary>
public sealed class RenewalTests(ReferenceHostFactory factory) : ReferenceHostTest(factory)
{
    [Fact]
    public async Task A_student_renews_once_for_another_loan_period()
    {
        var w = await BuildAsync(Factory, Admin());
        await BookWithCopiesAsync(Admin(), "Compilers", null, "LIB-0001");
        var loanId = (Guid)(await LentAsync(Admin(), "LIB-0001", w.Ahmed))["id"]!;
        Factory.Clock.Advance(TimeSpan.FromDays(10));
        var now = Factory.Clock.GetUtcNow();

        var renewed = await w.AhmedClient.PostAsync($"/api/library/loans/{loanId}/renew", null);

        Assert.Equal(HttpStatusCode.OK, renewed.StatusCode);
        var loan = await BodyOf(renewed);
        Assert.Equal(1, (int)loan["renewals"]!);
        Assert.InRange(((DateTimeOffset)loan["dueAt"]! - now).TotalDays, 13.99, 14.01);
        Assert.Equal(HttpStatusCode.Conflict, (await w.AhmedClient.PostAsync($"/api/library/loans/{loanId}/renew", null)).StatusCode);
    }

    [Fact]
    public async Task Overdue_or_someone_elses_loans_are_not_renewed()
    {
        var w = await BuildAsync(Factory, Admin());
        await BookWithCopiesAsync(Admin(), "Compilers", null, "LIB-0001");
        var loanId = (Guid)(await LentAsync(Admin(), "LIB-0001", w.Ahmed))["id"]!;

        Assert.Equal(HttpStatusCode.NotFound, (await w.MonaClient.PostAsync($"/api/library/loans/{loanId}/renew", null)).StatusCode);

        Factory.Clock.Advance(TimeSpan.FromDays(15));
        Assert.Equal(HttpStatusCode.Conflict, (await w.AhmedClient.PostAsync($"/api/library/loans/{loanId}/renew", null)).StatusCode);
    }

    [Fact]
    public async Task A_waiting_reservation_blocks_renewal()
    {
        var w = await BuildAsync(Factory, Admin());
        var bookId = await BookWithCopiesAsync(Admin(), "Compilers", null, "LIB-0001");
        var loanId = (Guid)(await LentAsync(Admin(), "LIB-0001", w.Ahmed))["id"]!;
        Assert.Equal(HttpStatusCode.Created, (await ReserveAsync(w.MonaClient, bookId)).StatusCode);

        Assert.Equal(HttpStatusCode.Conflict, (await w.AhmedClient.PostAsync($"/api/library/loans/{loanId}/renew", null)).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");
}
