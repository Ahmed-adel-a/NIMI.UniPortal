// ACCEPTANCE TEST — written by the consultant for phase P11. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Library.LibrarySetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Library;

/// <summary>P11 Library — loan rules per role are data Student Affairs edits (no hardcoded university rules).</summary>
public sealed class LoanPolicyTests(ReferenceHostFactory factory) : ReferenceHostTest(factory)
{
    [Fact]
    public async Task The_defaults_are_returned_until_changed()
    {
        await BuildAsync(Factory, Admin());

        var policies = (await Admin().GetFromJsonAsync<JsonArray>("/api/admin/library/policies"))!;

        var student = policies.Single(p => (string?)p!["role"] == "student")!;
        Assert.Equal(14, (int)student["loanDays"]!);
        Assert.Equal(3, (int)student["maxOpenLoans"]!);
        Assert.Equal(1, (int)student["maxRenewals"]!);
        Assert.Equal(3, (int)student["holdDays"]!);
        var professor = policies.Single(p => (string?)p!["role"] == "professor")!;
        Assert.Equal(30, (int)professor["loanDays"]!);
        Assert.Equal(10, (int)professor["maxOpenLoans"]!);
        Assert.Equal(2, (int)professor["maxRenewals"]!);
    }

    [Fact]
    public async Task A_changed_rule_applies_to_the_next_loan()
    {
        var w = await BuildAsync(Factory, Admin());
        await BookWithCopiesAsync(Admin(), "Compilers", null, "LIB-0001");

        var changed = await Admin().PutAsJsonAsync("/api/admin/library/policies/student",
            new { loanDays = 7, maxOpenLoans = 2, maxRenewals = 0, holdDays = 2 });
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        var now = Factory.Clock.GetUtcNow();

        var loan = await LentAsync(Admin(), "LIB-0001", w.Ahmed);

        Assert.InRange(((DateTimeOffset)loan["dueAt"]! - now).TotalDays, 6.99, 7.01);
        Assert.Equal(HttpStatusCode.Conflict, (await w.AhmedClient.PostAsync($"/api/library/loans/{(Guid)loan["id"]!}/renew", null)).StatusCode);
    }

    [Fact]
    public async Task Rules_are_validated_and_only_student_affairs_changes_them()
    {
        var w = await BuildAsync(Factory, Admin());

        var zero = await Admin().PutAsJsonAsync("/api/admin/library/policies/student", new { loanDays = 0, maxOpenLoans = 3, maxRenewals = 1, holdDays = 3 });
        Assert.Equal(HttpStatusCode.BadRequest, zero.StatusCode);
        Assert.Contains("loanDays", await FieldsOf(zero));
        Assert.Equal(HttpStatusCode.NotFound, (await Admin().PutAsJsonAsync("/api/admin/library/policies/alumni",
            new { loanDays = 7, maxOpenLoans = 1, maxRenewals = 0, holdDays = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await w.HanyClient.GetAsync("/api/admin/library/policies")).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");
}
