using NIMI.UniPortal.Modules.Users.Domain.Accounts;
using NIMI.UniPortal.Modules.Users.Domain.Organization;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.UnitTests.Users;

/// <summary>Decision ROLES2: department heads belong to one active department and count as teaching staff.</summary>
public sealed class DepartmentHeadRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 24, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_department_head_belongs_to_its_department_and_must_change_the_first_password()
    {
        var department = NewDepartment();

        var head = User.CreateDepartmentHead("Dr. Hala Nabil", "hala@cu.edu.eg", "hash", department, null, new PersonalDetails(null, null, null, null), Now);

        Assert.Equal(UserRole.DepartmentHead, head.Role);
        Assert.Equal(department.Id, head.DepartmentId);
        Assert.True(head.MustChangePassword);
        Assert.Null(head.StudentCode);
    }

    [Fact]
    public void A_department_head_cannot_join_a_deactivated_department()
    {
        var faculty = Faculty.Create("ENG", "Engineering", "الهندسة", Now);
        var department = Department.Create(faculty, "CS", "Computer Science", "علوم الحاسب", null, Now);
        department.Update(faculty, "CS", "Computer Science", "علوم الحاسب", null, isActive: false, Now);

        var error = Assert.Throws<ArgumentException>(() =>
            User.CreateDepartmentHead("Head", "head@cu.edu.eg", "hash", department, AcademicTitle.Professor, new PersonalDetails(null, null, null, null), Now));
        Assert.Equal("departmentId", error.ParamName);
    }

    [Theory]
    [InlineData("professor", true)]
    [InlineData("department_head", true)]
    [InlineData("student", false)]
    [InlineData("admin", false)]
    public void Professors_and_department_heads_are_teaching_staff(string role, bool canTeach) =>
        Assert.Equal(canTeach, PortalRoles.CanTeach(role));

    private static Department NewDepartment() =>
        Department.Create(Faculty.Create("ENG", "Engineering", "الهندسة", Now), "CS", "Computer Science", "علوم الحاسب", null, Now);
}
