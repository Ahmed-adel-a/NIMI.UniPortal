// ACCEPTANCE TEST — written by the consultant for phase P09. Do not modify; report problems instead.
using System.Net;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Lifecycle;
using static NIMI.UniPortal.Modules.IntegrationTests.Grades.GradesSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Lifecycle.RegistrationSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Grades;

/// <summary>P9.2 — Student Affairs confirms complete groups and publishes; students see nothing before (D3, R1, R3, R4).</summary>
public sealed class ConfirmPublishTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task Confirming_needs_a_grade_for_every_roster_student()
    {
        var w = await Campus();
        await DraftedAsync(w.HanyClient, w.Cs301Group1, (w.Ahmed, 90m));

        var incomplete = await ConfirmAsync(Admin(), w.Cs301Group1);

        Assert.Equal(HttpStatusCode.BadRequest, incomplete.StatusCode);
        var details = (await BodyOf(incomplete))["details"]!.AsArray();
        var missing = Assert.Single(details)!;
        Assert.Equal("missingStudentIds", (string?)missing["field"]);
        Assert.Equal(w.Mona.ToString(), (string?)missing["error"]);
        Assert.Equal("draft", (string?)Row(await GroupGradesAsync(Admin(), w.Cs301Group1), w.Ahmed)["status"]);

        await DraftedAsync(w.HanyClient, w.Cs301Group1, (w.Mona, 75m));
        Assert.Equal(2, await ConfirmedAsync(Admin(), w.Cs301Group1));
        Assert.Equal("confirmed", (string?)Row(await GroupGradesAsync(Admin(), w.Cs301Group1), w.Mona)["status"]);
    }

    [Fact]
    public async Task Students_see_no_grades_before_publication()
    {
        var w = await Campus();
        await DraftedAsync(w.HanyClient, w.Cs301Group1, (w.Ahmed, 90m), (w.Mona, 75m));
        Assert.Empty((await MyGradesAsync(w.AhmedClient))["terms"]!.AsArray());

        await ConfirmedAsync(Admin(), w.Cs301Group1);

        var mine = await MyGradesAsync(w.AhmedClient);
        Assert.Empty(mine["terms"]!.AsArray());
        Assert.Null(mine["cumulativeGpa"]);
    }

    [Fact]
    public async Task Publishing_shows_the_grades_and_notifies_each_student_once()
    {
        var w = await Campus();
        await DraftedAsync(w.HanyClient, w.Cs301Group1, (w.Ahmed, 90m), (w.Mona, 75m));
        await ConfirmedAsync(Admin(), w.Cs301Group1);
        var ahmedBefore = await UnreadAsync(w.AhmedClient);
        var monaBefore = await UnreadAsync(w.MonaClient);

        Assert.Equal(2, await PublishedAsync(Admin(), w.TermId));

        var term = (await MyGradesAsync(w.AhmedClient))["terms"]!.AsArray().Single()!;
        Assert.Equal(w.TermId, (Guid)term["termId"]!);
        var grade = term["grades"]!.AsArray().Single()!;
        Assert.Equal("CS301", (string?)grade["subjectCode"]);
        Assert.Equal("A-", (string?)grade["grade"]);
        Assert.Equal(ahmedBefore + 1, await UnreadAsync(w.AhmedClient));
        Assert.Equal(monaBefore + 1, await UnreadAsync(w.MonaClient));
        Assert.Equal("published", (string?)Row(await GroupGradesAsync(w.HanyClient, w.Cs301Group1), w.Ahmed)["status"]);
    }

    [Fact]
    public async Task Only_confirmed_grades_are_published_and_only_once()
    {
        var w = await Campus();
        await DraftedAsync(w.HanyClient, w.Cs301Group1, (w.Ahmed, 90m), (w.Mona, 75m));

        Assert.Equal(0, await PublishedAsync(Admin(), w.TermId));      // drafts stay drafts

        await ConfirmedAsync(Admin(), w.Cs301Group1);
        Assert.Equal(2, await PublishedAsync(Admin(), w.TermId, subjectId: w.Cs301));
        Assert.Equal(0, await PublishedAsync(Admin(), w.TermId));
    }

    [Fact]
    public async Task Publishing_can_be_limited_to_one_group()
    {
        var w = await BuildCampusAsync(Factory, Admin());
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);
        await RegisteredAsync(w.MonaClient, w.Cs301Group2);
        await DraftedAsync(w.HanyClient, w.Cs301Group1, (w.Ahmed, 90m));
        await DraftedAsync(w.HanyClient, w.Cs301Group2, (w.Mona, 75m));
        await ConfirmedAsync(Admin(), w.Cs301Group1);
        await ConfirmedAsync(Admin(), w.Cs301Group2);

        Assert.Equal(1, await PublishedAsync(Admin(), w.TermId, groupId: w.Cs301Group1));

        Assert.Single((await MyGradesAsync(w.AhmedClient))["terms"]!.AsArray());
        Assert.Empty((await MyGradesAsync(w.MonaClient))["terms"]!.AsArray());
    }

    [Fact]
    public async Task Published_grades_are_frozen_for_the_teacher()
    {
        var w = await Campus();
        await GradeAndPublishAsync(w.HanyClient, Admin(), w.TermId, w.Cs301Group1, (w.Ahmed, 90m), (w.Mona, 75m));

        Assert.Equal(HttpStatusCode.Conflict, (await PutDraftsAsync(w.HanyClient, w.Cs301Group1, (w.Ahmed, 99m))).StatusCode);
        Assert.Equal(0, await ConfirmedAsync(Admin(), w.Cs301Group1));    // nothing left to confirm
    }

    [Fact]
    public async Task Only_student_affairs_confirms_and_publishes()
    {
        var w = await Campus();
        await DraftedAsync(w.HanyClient, w.Cs301Group1, (w.Ahmed, 90m), (w.Mona, 75m));

        Assert.Equal(HttpStatusCode.Forbidden, (await ConfirmAsync(w.HanyClient, w.Cs301Group1)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await PublishAsync(w.HanyClient, w.TermId)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await PublishAsync(w.AhmedClient, w.TermId)).StatusCode);
    }

    [Fact]
    public async Task Publishing_needs_a_term_and_confirming_a_known_group()
    {
        var w = await Campus();

        var noTerm = await PublishAsync(Admin(), null);
        Assert.Equal(HttpStatusCode.BadRequest, noTerm.StatusCode);
        Assert.Contains("termId", await FieldsOf(noTerm));
        Assert.Equal(HttpStatusCode.NotFound, (await ConfirmAsync(Admin(), Guid.CreateVersion7())).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private Task<CampusWorld> Campus() => GradedCampusAsync(Factory, Admin());
}
