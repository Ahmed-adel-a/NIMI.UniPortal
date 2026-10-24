// ACCEPTANCE TEST — written by the consultant for decision FRONTEND (teaching classes). Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Users;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Scheduling;

/// <summary>
/// Decision FRONTEND: the groups a member of the teaching staff teaches in the active term, with ids, so the app can
/// open attendance, draft grades and post per class. A whole-subject assignment covers every group of the subject.
/// </summary>
public sealed class TeachingClassesTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task Teaching_staff_see_the_groups_they_teach_this_term()
    {
        var admin = Factory.CreateClientAs("admin");
        var eng = await OrganizationSetup.CreateFacultyAsync(admin, "ENG");
        var cs = await OrganizationSetup.CreateDepartmentAsync(admin, eng, "CS");
        var (hany, hanyToken) = await AccountSetup.CreateProfessorWithTokenAsync(Factory, admin, cs, "hany@cu.edu.eg");
        var (mona, monaToken) = await AccountSetup.CreateProfessorWithTokenAsync(Factory, admin, cs, "mona@cu.edu.eg");
        var termId = await CreateActiveTermAsync(admin);
        var cs101 = await CreateSubjectAsync(admin, cs, "CS101", "Programming", "البرمجة");
        var cs201 = await CreateSubjectAsync(admin, cs, "CS201", "Data structures", "هياكل البيانات");
        var cs101G1 = await CreateGroupAsync(admin, cs101, termId, groupNumber: 1);
        var cs101G2 = await CreateGroupAsync(admin, cs101, termId, groupNumber: 2);
        var cs201G1 = await CreateGroupAsync(admin, cs201, termId, groupNumber: 1);
        await AssignTeacherAsync(admin, hany, cs101, termId);            // the whole subject
        await AssignTeacherAsync(admin, mona, cs201, termId, cs201G1);   // one group

        var hanys = (await Factory.CreateClientWithToken(hanyToken).GetFromJsonAsync<JsonArray>("/api/teaching/classes"))!;
        Assert.Equal(new[] { cs101G1, cs101G2 }.Order(), hanys.Select(c => (Guid)c!["groupId"]!).Order());
        Assert.All(hanys, c => Assert.Equal("CS101", (string?)c!["subjectCode"]));

        var monas = (await Factory.CreateClientWithToken(monaToken).GetFromJsonAsync<JsonArray>("/api/teaching/classes"))!;
        var only = monas.Single()!;
        Assert.Equal(cs201G1, (Guid)only["groupId"]!);
        Assert.Equal(cs201, (Guid)only["subjectId"]!);
        Assert.Equal(1, (int)only["groupNumber"]!);

        Assert.Equal(HttpStatusCode.Forbidden, (await Factory.CreateClientAs("student").GetAsync("/api/teaching/classes")).StatusCode);
    }
}
