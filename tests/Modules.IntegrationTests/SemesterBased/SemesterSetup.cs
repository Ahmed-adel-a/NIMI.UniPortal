// ACCEPTANCE TEST — written by the consultant for phase P4.B. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Users;
using static NIMI.UniPortal.Modules.IntegrationTests.Grades.GradesSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.SemesterBased;

/// <summary>SemesterBased (نظام الفرق) helpers through the reference host's API (P4.B, decision SEMPROMO).</summary>
internal static class SemesterSetup
{
    public const string SecondTermStart = "2027-02-06";
    public const string SecondTermEnd = "2027-06-11";

    public static async Task<SemesterWorld> BuildAsync(ReferenceHostFactory factory, HttpClient admin, bool withPlan = true)
    {
        var eng = await OrganizationSetup.CreateFacultyAsync(admin, "ENG");
        var cs = await OrganizationSetup.CreateDepartmentAsync(admin, eng, "CS");
        var (ahmed, ahmedToken) = await AccountSetup.CreateStudentWithTokenAsync(factory, admin, cs, "ahmed@asu.edu.eg");
        var (mona, monaToken) = await AccountSetup.CreateStudentWithTokenAsync(factory, admin, cs, "mona@asu.edu.eg");
        var (hany, hanyToken) = await AccountSetup.CreateProfessorWithTokenAsync(factory, admin, cs, "hany@asu.edu.eg");

        var termId = await SecondTermAsync(admin, 2026, SecondTermStart, SecondTermEnd);
        var subjects = new Dictionary<string, Guid>();
        foreach (var code in new[] { "S1", "S2", "S3", "S4", "F1", "T1" })
            subjects[code] = await CreateSubjectAsync(admin, cs, code, $"Subject {code}", $"مادة {code}");

        var groups = await GroupsAsync(admin, termId, subjects, hany);
        if (withPlan)
            Assert.Equal(HttpStatusCode.OK, (await SavePlanAsync(admin, cs, 4, PlanOf(subjects))).StatusCode);

        return new SemesterWorld(
            eng, cs, termId, subjects, groups,
            ahmed, factory.CreateClientWithToken(ahmedToken),
            mona, factory.CreateClientWithToken(monaToken),
            hany, factory.CreateClientWithToken(hanyToken));
    }

    /// <summary>Creates and activates a second term (closing the active one first, if any).</summary>
    public static async Task<Guid> SecondTermAsync(HttpClient admin, int academicYear, string startsOn, string endsOn)
    {
        var current = await admin.GetAsync("/api/terms/current");
        if (current.StatusCode == HttpStatusCode.OK)
        {
            var currentId = (Guid)(await BodyOf(current))["id"]!;
            Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/admin/terms/{currentId}/close", null)).StatusCode);
        }

        var termId = await CreateTermAsync(admin, academicYear, "second", startsOn, endsOn, 18);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/admin/terms/{termId}/activate", null)).StatusCode);
        return termId;
    }

    /// <summary>One group per subject (two for S1), Hany teaching each subject; keys are "S1", "S1#2", "S2", …</summary>
    public static async Task<Dictionary<string, Guid>> GroupsAsync(
        HttpClient admin, Guid termId, IReadOnlyDictionary<string, Guid> subjects, Guid teacherId)
    {
        var groups = new Dictionary<string, Guid>();
        foreach (var (code, subjectId) in subjects)
        {
            groups[code] = await CreateGroupAsync(admin, subjectId, termId, 1, 60);
            await AssignTeacherAsync(admin, teacherId, subjectId, termId);
        }

        groups["S1#2"] = await CreateGroupAsync(admin, subjects["S1"], termId, 2, 60);
        return groups;
    }

    public static object[] PlanOf(IReadOnlyDictionary<string, Guid> s) =>
    [
        new { level = 1, term = "second", subjectId = s["S1"] },
        new { level = 1, term = "second", subjectId = s["S2"] },
        new { level = 1, term = "second", subjectId = s["S3"] },
        new { level = 1, term = "second", subjectId = s["S4"] },
        new { level = 1, term = "first", subjectId = s["F1"] },
        new { level = 2, term = "second", subjectId = s["T1"] },
    ];

    public static async Task<HttpResponseMessage> SavePlanAsync(HttpClient admin, Guid departmentId, int levels, object[] subjects) =>
        await admin.PutAsJsonAsync($"/api/admin/study-plans/{departmentId}", new { levels, subjects });

    public static async Task<HttpResponseMessage> SavePolicyAsync(
        HttpClient admin, int maxCarriedSubjects = 3, int[]? gateLevels = null, int maxYearsPerLevel = 2, bool requireFeesPaid = true,
        Guid? facultyId = null) =>
        await admin.PutAsJsonAsync(
            facultyId is null ? "/api/admin/promotion-policy" : $"/api/admin/faculties/{facultyId}/promotion-policy",
            new { maxCarriedSubjects, gateLevels = gateLevels ?? [], maxYearsPerLevel, requireFeesPaid });

    public static async Task<HttpResponseMessage> GenerateAsync(HttpClient admin, Guid termId, Guid? departmentId = null) =>
        await admin.PostAsJsonAsync("/api/admin/term-enrollments/generate", new { termId, departmentId });

    public static async Task<JsonObject> GeneratedAsync(HttpClient admin, Guid termId, Guid? departmentId = null)
    {
        var response = await GenerateAsync(admin, termId, departmentId);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await BodyOf(response);
    }

    /// <summary>The student's enrollments in a term (subject id → group id + carried flag).</summary>
    public static async Task<JsonArray> EnrollmentsAsync(HttpClient admin, Guid termId, Guid studentId) =>
        (await admin.GetFromJsonAsync<JsonObject>($"/api/admin/term-enrollments?termId={termId}&studentId={studentId}&pageSize=100"))!
            ["items"]!.AsArray();

    public static async Task<JsonObject> StandingAsync(HttpClient student) =>
        (await student.GetFromJsonAsync<JsonObject>("/api/standing"))!;

    /// <summary>Grades every level-1 second-term subject (one score per student and subject) and publishes them.</summary>
    public static async Task GradeYearAsync(
        SemesterWorld w, HttpClient admin, IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, decimal>> scores)
    {
        foreach (var code in new[] { "S1", "S2", "S3", "S4" })
        {
            foreach (var groupKey in new[] { code, code + "#2" }.Where(w.Groups.ContainsKey))
            {
                var groupId = w.Groups[groupKey];
                var roster = (await w.HanyClient.GetFromJsonAsync<JsonObject>($"/api/teaching/groups/{groupId}/roster?pageSize=100"))!
                    ["items"]!.AsArray().Select(r => (Guid)r!["studentId"]!).ToList();
                if (roster.Count == 0)
                    continue;
                var entries = roster.Select(id => (id, scores[id][code])).ToArray();
                await GradeAndPublishAsync(w.HanyClient, admin, w.TermId, groupId, entries);
            }
        }
    }

    public static IReadOnlyDictionary<string, decimal> Scores(decimal s1, decimal s2, decimal s3, decimal s4) =>
        new Dictionary<string, decimal> { ["S1"] = s1, ["S2"] = s2, ["S3"] = s3, ["S4"] = s4 };

    public static IReadOnlyDictionary<string, decimal> AllPassed => Scores(85, 85, 85, 85);

    public static async Task<string> NotificationTitlesAsync(HttpClient client) =>
        await client.GetStringAsync("/api/notifications/mine?pageSize=100");
}
