// ACCEPTANCE TEST — written by the consultant for phase P09. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.IntegrationTests.Users;

/// <summary>P9.0 — the Users contract read graduation eligibility needs: active students per university, faculty or department.</summary>
public sealed class StudentListingTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task Active_students_are_listed_per_university_faculty_or_department_ordered_by_code()
    {
        var o = await Organization();
        var (ahmed, _) = await AccountSetup.CreateStudentWithTokenAsync(Factory, Admin(), o.Cs, "ahmed@cu.edu.eg");
        var (mona, _) = await AccountSetup.CreateStudentWithTokenAsync(Factory, Admin(), o.Cs, "mona@cu.edu.eg");
        var (omar, _) = await AccountSetup.CreateStudentWithTokenAsync(Factory, Admin(), o.Civ, "omar@cu.edu.eg");
        var (sara, _) = await AccountSetup.CreateStudentWithTokenAsync(Factory, Admin(), o.Acc, "sara@cu.edu.eg");
        await AccountSetup.CreateProfessorWithTokenAsync(Factory, Admin(), o.Cs, "hany@cu.edu.eg");      // not a student
        var deactivate = await Admin().PatchAsJsonAsync($"/api/admin/users/{omar}/status", new { isActive = false });
        Assert.Equal(HttpStatusCode.NoContent, deactivate.StatusCode);

        var all = await UsersApi(api => api.GetStudentsAsync(null, null, new PageRequest(1, 20)));
        Assert.Equal(3, all.TotalCount);
        Assert.Equal(new[] { ahmed, mona, sara }.Order(), all.Items.Select(s => s.StudentId).Order());
        Assert.Equal(all.Items.Select(s => s.StudentCode).Order(StringComparer.Ordinal), all.Items.Select(s => s.StudentCode));

        var engineering = await UsersApi(api => api.GetStudentsAsync(o.Eng, null, new PageRequest(1, 20)));
        Assert.Equal([ahmed, mona], engineering.Items.Select(s => s.StudentId));

        var accounting = await UsersApi(api => api.GetStudentsAsync(o.Com, o.Acc, new PageRequest(1, 20)));
        Assert.Equal([sara], accounting.Items.Select(s => s.StudentId));

        Assert.Empty((await UsersApi(api => api.GetStudentsAsync(null, o.Civ, new PageRequest(1, 20)))).Items);
    }

    [Fact]
    public async Task A_listed_student_carries_name_code_and_placement()
    {
        var o = await Organization();
        var (ahmed, _) = await AccountSetup.CreateStudentWithTokenAsync(Factory, Admin(), o.Cs, "ahmed@cu.edu.eg");
        var user = await UsersApi(api => api.GetUserAsync(ahmed));

        var listed = (await UsersApi(api => api.GetStudentsAsync(null, o.Cs, new PageRequest(1, 20)))).Items.Single();

        Assert.Equal(ahmed, listed.StudentId);
        Assert.Equal(user!.FullName, listed.FullName);
        Assert.Equal(user.StudentCode, listed.StudentCode);
        Assert.Equal(o.Cs, listed.DepartmentId);
        Assert.Equal(o.Eng, listed.FacultyId);
        Assert.Equal(2026, listed.EnrollmentYear);
    }

    [Fact]
    public async Task The_listing_is_paged()
    {
        var o = await Organization();
        await AccountSetup.CreateStudentWithTokenAsync(Factory, Admin(), o.Cs, "s1@cu.edu.eg");
        await AccountSetup.CreateStudentWithTokenAsync(Factory, Admin(), o.Cs, "s2@cu.edu.eg");
        var (third, _) = await AccountSetup.CreateStudentWithTokenAsync(Factory, Admin(), o.Cs, "s3@cu.edu.eg");

        var page = await UsersApi(api => api.GetStudentsAsync(null, o.Cs, new PageRequest(2, 2)));

        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.Page);
        Assert.Equal(2, page.PageSize);
        Assert.Equal([third], page.Items.Select(s => s.StudentId));
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
