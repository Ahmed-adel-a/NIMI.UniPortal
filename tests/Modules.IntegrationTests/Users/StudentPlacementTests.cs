// ACCEPTANCE TEST — written by the consultant for phase P05-users. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.Users.Contracts;

namespace NIMI.UniPortal.Modules.IntegrationTests.Users;

/// <summary>P5.0 — the two Users contract reads Payments needs: where a student belongs, and how many active students there are.</summary>
public sealed class StudentPlacementTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task A_students_placement_names_department_faculty_and_year()
    {
        var o = await Organization();
        var (ahmed, _) = await AccountSetup.CreateStudentWithTokenAsync(Factory, Admin(), o.Cs, "ahmed@cu.edu.eg");

        var placement = await UsersApi(api => api.GetStudentPlacementAsync(ahmed));

        Assert.NotNull(placement);
        Assert.Equal(ahmed, placement.StudentId);
        Assert.Equal(o.Cs, placement.DepartmentId);
        Assert.Equal(o.Eng, placement.FacultyId);
        Assert.Equal(2026, placement.EnrollmentYear);
        Assert.True(placement.IsActive);
    }

    [Fact]
    public async Task Professors_and_unknown_ids_have_no_placement()
    {
        var o = await Organization();
        var (hany, _) = await AccountSetup.CreateProfessorWithTokenAsync(Factory, Admin(), o.Cs, "hany@cu.edu.eg");

        Assert.Null(await UsersApi(api => api.GetStudentPlacementAsync(hany)));
        Assert.Null(await UsersApi(api => api.GetStudentPlacementAsync(Guid.CreateVersion7())));
    }

    [Fact]
    public async Task Active_students_are_counted_per_university_faculty_or_department()
    {
        var o = await Organization();
        await AccountSetup.CreateStudentWithTokenAsync(Factory, Admin(), o.Cs, "ahmed@cu.edu.eg");
        await AccountSetup.CreateStudentWithTokenAsync(Factory, Admin(), o.Cs, "mona@cu.edu.eg");
        var (omar, _) = await AccountSetup.CreateStudentWithTokenAsync(Factory, Admin(), o.Civ, "omar@cu.edu.eg");
        await AccountSetup.CreateStudentWithTokenAsync(Factory, Admin(), o.Acc, "sara@cu.edu.eg");
        await AccountSetup.CreateProfessorWithTokenAsync(Factory, Admin(), o.Cs, "hany@cu.edu.eg");     // not a student
        var deactivate = await Admin().PatchAsJsonAsync($"/api/admin/users/{omar}/status", new { isActive = false });
        Assert.Equal(HttpStatusCode.NoContent, deactivate.StatusCode);

        Assert.Equal(3, await UsersApi(api => api.CountActiveStudentsAsync(null, null)));
        Assert.Equal(2, await UsersApi(api => api.CountActiveStudentsAsync(o.Eng, null)));
        Assert.Equal(2, await UsersApi(api => api.CountActiveStudentsAsync(null, o.Cs)));
        Assert.Equal(0, await UsersApi(api => api.CountActiveStudentsAsync(null, o.Civ)));
        Assert.Equal(1, await UsersApi(api => api.CountActiveStudentsAsync(o.Com, null)));
        Assert.Equal(2, await UsersApi(api => api.CountActiveStudentsAsync(o.Com, o.Cs)));       // the department wins
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private async Task<T> UsersApi<T>(Func<IUsersApi, Task<T>> call)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        return await call(scope.ServiceProvider.GetRequiredService<IUsersApi>());
    }

    private async Task<(Guid Eng, Guid Cs, Guid Civ, Guid Com, Guid Acc)> Organization()
    {
        var eng = await OrganizationSetup.CreateFacultyAsync(Admin(), "ENG");
        var com = await OrganizationSetup.CreateFacultyAsync(Admin(), "COM");
        return (
            eng,
            await OrganizationSetup.CreateDepartmentAsync(Admin(), eng, "CS"),
            await OrganizationSetup.CreateDepartmentAsync(Admin(), eng, "CIV"),
            com,
            await OrganizationSetup.CreateDepartmentAsync(Admin(), com, "ACC"));
    }
}
