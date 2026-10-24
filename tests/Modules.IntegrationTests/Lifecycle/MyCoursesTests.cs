// ACCEPTANCE TEST — written by the consultant for decision FRONTEND (student courses). Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Lifecycle.RegistrationSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Lifecycle;

/// <summary>
/// Decision FRONTEND: the student's subjects this term, the same for both academic systems (the lifecycle router
/// answers), so the app can show materials, announcements and assignments per course.
/// </summary>
public sealed class MyCoursesTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task A_student_sees_the_subjects_and_groups_they_study_this_term()
    {
        var admin = Factory.CreateClientAs("admin");
        var w = await MixedCampusSetup.BuildAsync(Factory, admin);
        await RegisteredAsync(w.AhmedClient, w.Cs101Group);
        await admin.PostAsJsonAsync("/api/admin/term-enrollments/generate", new { termId = w.TermId });

        var ahmed = (await w.AhmedClient.GetFromJsonAsync<JsonArray>("/api/courses/mine"))!;
        var cs101 = ahmed.Single()!;
        Assert.Equal("CS101", (string?)cs101["subjectCode"]);
        Assert.Equal(w.Cs101Group, (Guid)cs101["groupId"]!);
        Assert.Equal(3, (int)cs101["creditHours"]!);

        var sara = (await w.SaraClient.GetFromJsonAsync<JsonArray>("/api/courses/mine"))!;
        Assert.Equal("ACC101", (string?)sara.Single()!["subjectCode"]);   // year groups: generated enrollment

        Assert.Equal(HttpStatusCode.Forbidden, (await w.HanyClient.GetAsync("/api/courses/mine")).StatusCode);
    }
}
