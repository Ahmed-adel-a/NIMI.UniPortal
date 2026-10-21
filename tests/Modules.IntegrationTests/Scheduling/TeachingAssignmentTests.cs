// ACCEPTANCE TEST — written by the consultant for phase P03. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Users;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Scheduling;

/// <summary>P3.3 — who teaches what (decision MULTIPROF, rule R5).</summary>
public sealed class TeachingAssignmentTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task Admin_assigns_a_professor_to_a_whole_subject()
    {
        var s = await Setup();

        var response = await PostAssignmentAsync(Admin(), s.ProfessorId, s.SubjectId, s.TermId);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var assignment = await BodyOf(response);
        Assert.Equal(s.ProfessorId, (Guid)assignment["staffId"]!);
        Assert.Equal("Test Professor", (string?)assignment["staffName"]);
        Assert.Equal("CS301", (string?)assignment["subjectCode"]);
        Assert.Null(assignment["subjectGroupId"]);
        Assert.Equal("lecturer", (string?)assignment["role"]);
        Assert.True((bool)assignment["isActive"]!);
        var listed = (await Admin().GetFromJsonAsync<JsonObject>($"/api/admin/teaching-assignments?staffId={s.ProfessorId}&termId={s.TermId}"))!;
        Assert.Equal(1, (int)listed["totalCount"]!);
    }

    [Fact]
    public async Task Several_staff_can_share_a_subject_and_one_can_take_a_single_group()
    {
        var s = await Setup();
        var (secondProfessor, _) = await AccountSetup.CreateProfessorWithTokenAsync(Factory, Admin(), s.DepartmentId, "mona.prof@cu.edu.eg");

        var lecturer = await PostAssignmentAsync(Admin(), s.ProfessorId, s.SubjectId, s.TermId);
        var lab = await PostAssignmentAsync(Admin(), secondProfessor, s.SubjectId, s.TermId, s.GroupId, "lab");

        Assert.Equal(HttpStatusCode.Created, lecturer.StatusCode);
        Assert.Equal(HttpStatusCode.Created, lab.StatusCode);
        var labAssignment = await BodyOf(lab);
        Assert.Equal(s.GroupId, (Guid)labAssignment["subjectGroupId"]!);
        Assert.Equal(1, (int)labAssignment["groupNumber"]!);
        Assert.Equal("lab", (string?)labAssignment["role"]);
        var listed = (await Admin().GetFromJsonAsync<JsonObject>($"/api/admin/teaching-assignments?subjectId={s.SubjectId}"))!;
        Assert.Equal(2, (int)listed["totalCount"]!);
    }

    [Fact]
    public async Task Only_an_active_professor_can_be_assigned()
    {
        var s = await Setup();
        var (studentId, _) = await AccountSetup.CreateStudentWithTokenAsync(Factory, Admin(), s.DepartmentId, "ahmed@cu.edu.eg");

        var student = await PostAssignmentAsync(Admin(), studentId, s.SubjectId, s.TermId);
        var unknown = await PostAssignmentAsync(Admin(), Guid.CreateVersion7(), s.SubjectId, s.TermId);

        Assert.Equal(HttpStatusCode.BadRequest, student.StatusCode);
        Assert.Contains("staffId", await FieldsOf(student));
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Contains("staffId", await FieldsOf(unknown));
    }

    [Fact]
    public async Task The_same_assignment_twice_is_a_409()
    {
        var s = await Setup();
        await AssignTeacherAsync(Admin(), s.ProfessorId, s.SubjectId, s.TermId);

        var duplicate = await PostAssignmentAsync(Admin(), s.ProfessorId, s.SubjectId, s.TermId);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task A_group_of_another_subject_is_a_400()
    {
        var s = await Setup();
        var otherSubject = await CreateSubjectAsync(Admin(), s.DepartmentId, "CS302");
        var otherGroup = await CreateGroupAsync(Admin(), otherSubject, s.TermId);

        var response = await PostAssignmentAsync(Admin(), s.ProfessorId, s.SubjectId, s.TermId, otherGroup);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("subjectGroupId", await FieldsOf(response));
    }

    [Fact]
    public async Task Removing_an_assignment_deactivates_it()
    {
        var s = await Setup();
        var id = await AssignTeacherAsync(Admin(), s.ProfessorId, s.SubjectId, s.TermId);

        var response = await Admin().DeleteAsync($"/api/admin/teaching-assignments/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var listed = (await Admin().GetFromJsonAsync<JsonObject>($"/api/admin/teaching-assignments?staffId={s.ProfessorId}"))!;
        Assert.False((bool)listed["items"]![0]!["isActive"]!);
        Assert.Equal(HttpStatusCode.NotFound, (await Admin().DeleteAsync($"/api/admin/teaching-assignments/{Guid.CreateVersion7()}")).StatusCode);
    }

    [Fact]
    public async Task Only_admins_manage_teaching_assignments()
    {
        var s = await Setup();
        var professor = Factory.CreateClientAs("professor");

        Assert.Equal(HttpStatusCode.Forbidden, (await professor.GetAsync("/api/admin/teaching-assignments")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await professor.PostAsJsonAsync("/api/admin/teaching-assignments",
            new { staffId = s.ProfessorId, subjectId = s.SubjectId, termId = s.TermId, role = "lecturer" })).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private async Task<(Guid DepartmentId, Guid SubjectId, Guid TermId, Guid GroupId, Guid ProfessorId)> Setup()
    {
        var cs = await OrganizationSetup.CreateDepartmentAsync(Admin(), await OrganizationSetup.CreateFacultyAsync(Admin(), "ENG"), "CS");
        var subjectId = await CreateSubjectAsync(Admin(), cs, "CS301");
        var termId = await CreateActiveTermAsync(Admin());
        var groupId = await CreateGroupAsync(Admin(), subjectId, termId);
        var (professorId, _) = await AccountSetup.CreateProfessorWithTokenAsync(Factory, Admin(), cs, "hany@cu.edu.eg");
        return (cs, subjectId, termId, groupId, professorId);
    }
}
