// ACCEPTANCE TEST — written by the consultant for phase P04A. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Lifecycle.RegistrationSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Lifecycle;

/// <summary>P4.6 — the student's progress (credits earned arrive with grades in P9; MILEDU outstanding requirements now).</summary>
public sealed class ProgressTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task Progress_shows_credits_in_progress_and_outstanding_graduation_requirements()
    {
        var w = await Campus();
        await RegisteredAsync(w.AhmedClient, w.Cs301Group1);
        await RegisteredAsync(w.AhmedClient, w.Cs303Group1);

        var progress = (await w.AhmedClient.GetFromJsonAsync<JsonObject>("/api/registration/progress"))!;

        Assert.Equal(w.Ahmed, (Guid)progress["studentId"]!);
        Assert.Equal(0, (int)progress["creditsEarned"]!);
        Assert.Equal(6, (int)progress["creditsInProgress"]!);
        Assert.Null(progress["cumulativeGpa"]);
        var outstanding = progress["outstandingRequirements"]!.AsArray().Single()!;
        Assert.Equal(w.Mil100, (Guid)outstanding["subjectId"]!);
        Assert.Equal("MIL100", (string?)outstanding["code"]);
        Assert.Equal("التربية العسكرية", (string?)outstanding["nameAr"]);
    }

    [Fact]
    public async Task Dropped_registrations_do_not_count()
    {
        var w = await Campus();
        var registration = await RegisteredAsync(w.AhmedClient, w.Cs301Group1);
        await w.AhmedClient.DeleteAsync($"/api/registration/{registration}");

        var progress = (await w.AhmedClient.GetFromJsonAsync<JsonObject>("/api/registration/progress"))!;

        Assert.Equal(0, (int)progress["creditsInProgress"]!);
    }

    [Fact]
    public async Task Only_students_have_progress()
    {
        var w = await Campus();

        Assert.Equal(HttpStatusCode.Forbidden, (await w.HanyClient.GetAsync("/api/registration/progress")).StatusCode);
    }

    private Task<CampusWorld> Campus() => BuildCampusAsync(Factory, Factory.CreateClientAs("admin"));
}
