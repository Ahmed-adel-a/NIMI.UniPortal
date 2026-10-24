// ACCEPTANCE TEST — written by the consultant for decision LIFECYCLEDEPT. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Users;
using static NIMI.UniPortal.Modules.IntegrationTests.Lifecycle.RegistrationSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Lifecycle;

/// <summary>The mixed campus of <see cref="MixedCampus"/>, built through the API (shared by the lifecycle tests).</summary>
internal static class MixedCampusSetup
{
    public static async Task<MixedCampus> BuildAsync(PortalWebAppFactory factory, HttpClient admin)
    {
        var eng = await OrganizationSetup.CreateFacultyAsync(admin, "ENG");
        var cs = await OrganizationSetup.CreateDepartmentAsync(admin, eng, "CS");
        var com = await OrganizationSetup.CreateFacultyAsync(admin, "COM");
        var acc = await OrganizationSetup.CreateDepartmentAsync(admin, com, "ACC");
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/admin/faculties/{com}/academic-system", new { academicSystem = "year_groups" })).StatusCode);

        var (ahmed, ahmedToken) = await AccountSetup.CreateStudentWithTokenAsync(factory, admin, cs, "ahmed@cu.edu.eg");
        var (sara, saraToken) = await AccountSetup.CreateStudentWithTokenAsync(factory, admin, acc, "sara@cu.edu.eg");
        var (hany, hanyToken) = await AccountSetup.CreateProfessorWithTokenAsync(factory, admin, cs, "hany@cu.edu.eg");

        var termId = await CreateActiveTermAsync(admin);
        await OpenWindowAsync(factory, admin, termId, minCredits: 3);
        var hall = await CreateFacilityAsync(admin, "Hall A", eng);

        var cs101 = await CreateSubjectAsync(admin, cs, "CS101", "Programming", "البرمجة");
        var acc101 = await CreateSubjectAsync(admin, acc, "ACC101", "Accounting", "المحاسبة");
        var cs101Group = await CreateGroupAsync(admin, cs101, termId);
        var acc101Group = await CreateGroupAsync(admin, acc101, termId);
        await AssignTeacherAsync(admin, hany, cs101, termId);
        await AssignTeacherAsync(admin, hany, acc101, termId);
        await CreateSlotAsync(admin, cs101Group, hall, null, 1, "09:00:00", "10:30:00");
        await CreateSlotAsync(admin, acc101Group, hall, null, 2, "09:00:00", "10:30:00");

        var plan = await admin.PutAsJsonAsync($"/api/admin/study-plans/{acc}",
            new { levels = 4, subjects = new[] { new { level = 1, term = "first", subjectId = acc101 } } });
        Assert.Equal(HttpStatusCode.OK, plan.StatusCode);

        return new MixedCampus(
            eng, cs, com, acc, termId, cs101, cs101Group, acc101, acc101Group,
            ahmed, factory.CreateClientWithToken(ahmedToken),
            sara, factory.CreateClientWithToken(saraToken),
            hany, factory.CreateClientWithToken(hanyToken));
    }
}
