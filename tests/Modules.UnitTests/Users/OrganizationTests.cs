using NIMI.UniPortal.Modules.Users.Domain.Organization;

namespace NIMI.UniPortal.Modules.UnitTests.Users;

/// <summary>Faculty / department rules (codes end up inside student codes, so they stay short and clean).</summary>
public sealed class OrganizationTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 9, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("eng", "ENG")]
    [InlineData(" cs ", "CS")]
    [InlineData("ENG2", "ENG2")]
    public void Codes_are_trimmed_and_upper_cased(string input, string stored) =>
        Assert.Equal(stored, Faculty.Create(input, "Engineering", "الهندسة", Now).Code);

    [Theory]
    [InlineData("E")]
    [InlineData("TOOLONGCODE")]
    [InlineData("EN-G")]
    [InlineData("هندسة")]
    [InlineData("")]
    public void Codes_must_be_2_to_10_letters_or_digits(string code)
    {
        var error = Assert.Throws<ArgumentException>(() => Faculty.Create(code, "Engineering", "الهندسة", Now));
        Assert.Equal("code", error.ParamName);
    }

    [Fact]
    public void Both_names_are_required()
    {
        Assert.Equal("name", Assert.Throws<ArgumentException>(() => Faculty.Create("ENG", " ", "الهندسة", Now)).ParamName);
        Assert.Equal("nameAr", Assert.Throws<ArgumentException>(() => Faculty.Create("ENG", "Engineering", "", Now)).ParamName);
    }

    [Fact]
    public void A_new_faculty_is_active_with_a_v7_id()
    {
        var faculty = Faculty.Create("ENG", "Engineering", "الهندسة", Now);

        Assert.True(faculty.IsActive);
        Assert.Equal(7, faculty.Id.Version);
        Assert.Equal(Now, faculty.CreatedAt);
    }

    [Fact]
    public void No_department_can_be_opened_under_a_deactivated_faculty()
    {
        var faculty = Faculty.Create("OLD", "Old", "قديم", Now);
        faculty.Update("OLD", "Old", "قديم", isActive: false, Now);

        var error = Assert.Throws<ArgumentException>(() => Department.Create(faculty, "CS", "CS", "حاسب", null, Now));

        Assert.Equal("facultyId", error.ParamName);
    }

    [Fact]
    public void A_department_can_stay_in_its_faculty_after_the_faculty_is_deactivated_but_cannot_move_into_one()
    {
        var engineering = Faculty.Create("ENG", "Engineering", "الهندسة", Now);
        var closed = Faculty.Create("OLD", "Old", "قديم", Now);
        var department = Department.Create(engineering, "CS", "CS", "حاسب", null, Now);
        engineering.Update("ENG", "Engineering", "الهندسة", isActive: false, Now);
        closed.Update("OLD", "Old", "قديم", isActive: false, Now);

        department.Update(engineering, "CS", "Computer Science", "علوم الحاسب", "renamed", isActive: true, Now);   // same faculty: fine
        Assert.Throws<ArgumentException>(() => department.Update(closed, "CS", "CS", "حاسب", null, true, Now));    // moving in: refused
    }
}
