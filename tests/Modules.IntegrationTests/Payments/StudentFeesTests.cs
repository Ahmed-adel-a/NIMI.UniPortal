// ACCEPTANCE TEST — written by the consultant for phase P05-payments. Do not modify; report problems instead.
using System.Net;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Payments.PaymentsSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Payments;

/// <summary>P5.2 — what each student owes (applicable-fees rule, §6).</summary>
public sealed class StudentFeesTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task A_student_sees_university_faculty_and_department_fees_with_balances()
    {
        var w = await World();

        var fees = await FeesAsync(w.AhmedClient);

        Assert.Equal(["Lab fee", "CS club", "Tuition"], Names(fees));      // by due date, then name
        var tuition = Fee(fees, w.Tuition);
        Assert.Equal("المصروفات الدراسية", (string?)tuition["nameAr"]);
        Assert.Equal("tuition", (string?)tuition["category"]);
        Assert.Equal(5000m, (decimal)tuition["amountDue"]!);
        Assert.Equal(0m, (decimal)tuition["amountPaid"]!);
        Assert.Equal(5000m, (decimal)tuition["balance"]!);
        Assert.Equal("pending", (string?)tuition["status"]);
        Assert.Equal(5400m, (decimal)fees["totalBalance"]!);
    }

    [Fact]
    public async Task Each_student_sees_only_the_fees_that_target_them()
    {
        var w = await World();

        Assert.Equal(["Lab fee", "Tuition"], Names(await FeesAsync(w.OmarClient)));
        Assert.Equal(["Commerce fee", "Tuition"], Names(await FeesAsync(w.SaraClient)));
    }

    [Fact]
    public async Task Overdue_is_computed_from_the_due_date_and_the_balance()
    {
        var w = await World();

        var before = await FeesAsync(w.AhmedClient);
        await RecordedAsync(Admin(), w.Ahmed, w.LabFee, 300m);
        var after = await FeesAsync(w.AhmedClient);

        Assert.True((bool)Fee(before, w.LabFee)["isOverdue"]!);
        Assert.False((bool)Fee(before, w.Tuition)["isOverdue"]!);
        Assert.False((bool)Fee(after, w.LabFee)["isOverdue"]!);           // paid in full → no longer overdue
        Assert.Equal("paid", (string?)Fee(after, w.LabFee)["status"]);
    }

    [Fact]
    public async Task Filtering_by_term_keeps_that_terms_fees_and_the_annual_ones()
    {
        var w = await World();

        var fees = await FeesAsync(w.AhmedClient, $"?termId={w.TermId}");
        var otherTerm = await FeesAsync(w.AhmedClient, $"?termId={Guid.CreateVersion7()}");

        Assert.Equal(["Lab fee", "CS club", "Tuition"], Names(fees));
        Assert.Equal(["CS club"], Names(otherTerm));
    }

    [Fact]
    public async Task Only_students_have_fees()
    {
        var w = await World();

        Assert.Equal(HttpStatusCode.Forbidden, (await Factory.CreateClientAs("professor").GetAsync("/api/payments/fees")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Factory.CreateClient().GetAsync("/api/payments/fees")).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private Task<PaymentsWorld> World() => BuildAsync(Factory, Admin());

    private static List<string?> Names(JsonObject fees) => fees["fees"]!.AsArray().Select(f => (string?)f!["name"]).ToList();
}
