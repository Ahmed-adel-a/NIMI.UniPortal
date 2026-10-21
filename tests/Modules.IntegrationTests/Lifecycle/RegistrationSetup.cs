// ACCEPTANCE TEST — written by the consultant for phase P04A. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Users;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Lifecycle;

/// <summary>Registration helpers through the real API. Later phases (attendance, grades…) reuse <see cref="BuildCampusAsync"/>.</summary>
internal static class RegistrationSetup
{
    public static async Task<HttpResponseMessage> PostWindowAsync(
        HttpClient admin, Guid termId, DateTimeOffset opensAt, DateTimeOffset closesAt, int minCredits = 12, int maxCredits = 21) =>
        await admin.PostAsJsonAsync("/api/admin/registration-windows", new { termId, opensAt, closesAt, minCredits, maxCredits });

    /// <summary>A window that opened an hour ago and closes in a week (real clock + the test offset).</summary>
    public static async Task<Guid> OpenWindowAsync(
        PortalWebAppFactory factory, HttpClient admin, Guid termId, int minCredits = 12, int maxCredits = 21)
    {
        var now = factory.Clock.GetUtcNow();
        var response = await PostWindowAsync(admin, termId, now.AddHours(-1), now.AddDays(7), minCredits, maxCredits);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (Guid)(await BodyOf(response))["id"]!;
    }

    public static async Task<HttpResponseMessage> PutWindowAsync(
        HttpClient admin, Guid windowId, DateTimeOffset opensAt, DateTimeOffset closesAt, int minCredits = 12, int maxCredits = 21) =>
        await admin.PutAsJsonAsync($"/api/admin/registration-windows/{windowId}", new { opensAt, closesAt, minCredits, maxCredits });

    public static async Task<HttpResponseMessage> RegisterAsync(HttpClient student, Guid subjectGroupId) =>
        await student.PostAsJsonAsync("/api/registration", new { subjectGroupId });

    public static async Task<Guid> RegisteredAsync(HttpClient student, Guid subjectGroupId)
    {
        var response = await RegisterAsync(student, subjectGroupId);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (Guid)(await BodyOf(response))["id"]!;
    }

    public static async Task<HttpResponseMessage> SetPrerequisitesAsync(HttpClient admin, Guid subjectId, params Guid[] prerequisiteSubjectIds) =>
        await admin.PutAsJsonAsync($"/api/admin/registration/prerequisites/{subjectId}", new { prerequisiteSubjectIds });

    /// <summary>The <c>error</c> text of every error detail (<c>{ field, error }</c>; domain rules put their message there).</summary>
    public static async Task<List<string?>> MessagesOf(HttpResponseMessage response) =>
        (await BodyOf(response))["details"]!.AsArray().Select(d => (string?)d!["error"]).ToList();

    public static async Task<CampusWorld> BuildCampusAsync(PortalWebAppFactory factory, HttpClient admin)
    {
        var eng = await OrganizationSetup.CreateFacultyAsync(admin, "ENG");
        var cs = await OrganizationSetup.CreateDepartmentAsync(admin, eng, "CS");
        var civ = await OrganizationSetup.CreateDepartmentAsync(admin, eng, "CIV");
        var termId = await CreateActiveTermAsync(admin);
        var windowId = await OpenWindowAsync(factory, admin, termId);

        var cs301 = await CreateSubjectAsync(admin, cs, "CS301", "Algorithms", "الخوارزميات");
        var cs302 = await CreateSubjectAsync(admin, cs, "CS302", "Databases", "قواعد البيانات");
        var cs303 = await CreateSubjectAsync(admin, cs, "CS303", "Networks", "الشبكات");
        var civ101 = await CreateSubjectAsync(admin, civ, "CIV101", "Statics", "الاستاتيكا");
        var milResponse = await PostSubjectAsync(admin, cs, "MIL100", "Military education", "التربية العسكرية",
            creditHours: 0, category: "military_education", isGraduationRequirement: true);
        Assert.Equal(HttpStatusCode.Created, milResponse.StatusCode);
        var mil100 = (Guid)(await BodyOf(milResponse))["id"]!;

        var hallA = await CreateFacilityAsync(admin, "Hall A", eng);
        var hallB = await CreateFacilityAsync(admin, "Hall B", eng);

        var cs301Group1 = await CreateGroupAsync(admin, cs301, termId, 1);
        var cs301Group2 = await CreateGroupAsync(admin, cs301, termId, 2);
        var cs302Group1 = await CreateGroupAsync(admin, cs302, termId, 1);
        var cs303Group1 = await CreateGroupAsync(admin, cs303, termId, 1, capacity: 1);
        var civResponse = await PostGroupAsync(admin, civ101, termId, 1, 60, targetDepartmentId: civ);
        Assert.Equal(HttpStatusCode.Created, civResponse.StatusCode);
        var civ101Group1 = (Guid)(await BodyOf(civResponse))["id"]!;
        var mil100Group1 = await CreateGroupAsync(admin, mil100, termId, 1);

        await CreateSlotAsync(admin, cs301Group1, hallA, null, 1, "09:00:00", "10:30:00");
        await CreateSlotAsync(admin, cs301Group2, hallB, null, 2, "11:00:00", "12:30:00");
        await CreateSlotAsync(admin, cs302Group1, hallB, null, 1, "10:00:00", "11:00:00");
        await CreateSlotAsync(admin, cs303Group1, hallA, null, 3, "09:00:00", "10:30:00");
        await CreateSlotAsync(admin, civ101Group1, hallA, null, 4, "09:00:00", "10:30:00");
        await CreateSlotAsync(admin, mil100Group1, hallB, null, 5, "09:00:00", "10:30:00");

        var (hany, hanyToken) = await AccountSetup.CreateProfessorWithTokenAsync(factory, admin, cs, "hany@cu.edu.eg");
        await AssignTeacherAsync(admin, hany, cs301, termId);
        var (ahmed, ahmedToken) = await AccountSetup.CreateStudentWithTokenAsync(factory, admin, cs, "ahmed@cu.edu.eg");
        var (mona, monaToken) = await AccountSetup.CreateStudentWithTokenAsync(factory, admin, cs, "mona@cu.edu.eg");

        return new CampusWorld(
            FacultyId: eng,
            CsDepartmentId: cs,
            CivDepartmentId: civ,
            TermId: termId,
            WindowId: windowId,
            Cs301: cs301,
            Cs302: cs302,
            Cs303: cs303,
            Civ101: civ101,
            Mil100: mil100,
            Cs301Group1: cs301Group1,
            Cs301Group2: cs301Group2,
            Cs302Group1: cs302Group1,
            Cs303Group1: cs303Group1,
            Civ101Group1: civ101Group1,
            Mil100Group1: mil100Group1,
            Hany: hany,
            HanyClient: factory.CreateClientWithToken(hanyToken),
            Ahmed: ahmed,
            AhmedClient: factory.CreateClientWithToken(ahmedToken),
            Mona: mona,
            MonaClient: factory.CreateClientWithToken(monaToken));
    }

    public static async Task<JsonObject> MineAsync(HttpClient student) =>
        (await student.GetFromJsonAsync<JsonObject>("/api/registration/mine"))!;
}
