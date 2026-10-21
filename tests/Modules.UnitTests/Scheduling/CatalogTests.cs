// ACCEPTANCE TEST — written by the consultant for phase P03. Do not modify; report problems instead.
using NIMI.UniPortal.Modules.Scheduling.Domain.Catalog;
using static NIMI.UniPortal.Modules.UnitTests.Scheduling.SchedulingFixture;

namespace NIMI.UniPortal.Modules.UnitTests.Scheduling;

/// <summary>Subjects, groups and teaching assignments.</summary>
public sealed class CatalogTests
{
    [Fact]
    public void A_subject_code_is_trimmed_and_upper_cased()
    {
        var subject = NewSubject(" cs301 ");

        Assert.Equal("CS301", subject.Code);
        Assert.True(subject.IsActive);
        Assert.Equal(SubjectCategory.Regular, subject.Category);
    }

    [Fact]
    public void Invalid_subject_values_are_rejected_with_their_field_name()
    {
        var department = Guid.CreateVersion7();

        AssertRejected("code", () => Subject.Create(department, "C S", "A", "أ", null, 3, SubjectCategory.Regular, false, Now));
        AssertRejected("code", () => Subject.Create(department, "C", "A", "أ", null, 3, SubjectCategory.Regular, false, Now));
        AssertRejected("name", () => Subject.Create(department, "CS301", " ", "أ", null, 3, SubjectCategory.Regular, false, Now));
        AssertRejected("nameAr", () => Subject.Create(department, "CS301", "A", "", null, 3, SubjectCategory.Regular, false, Now));
        AssertRejected("creditHours", () => Subject.Create(department, "CS301", "A", "أ", null, 13, SubjectCategory.Regular, false, Now));
        AssertRejected("creditHours", () => Subject.Create(department, "CS301", "A", "أ", null, -1, SubjectCategory.Regular, false, Now));
    }

    [Fact]
    public void Military_education_is_a_subject_category_that_can_be_a_graduation_requirement()
    {
        var subject = Subject.Create(Guid.CreateVersion7(), "MIL100", "Military education", "التربية العسكرية", null, 0,
            SubjectCategory.MilitaryEducation, true, Now);

        Assert.Equal(SubjectCategory.MilitaryEducation, subject.Category);
        Assert.True(subject.IsGraduationRequirement);
        Assert.Equal(0, subject.CreditHours);
    }

    [Fact]
    public void A_group_belongs_to_its_subject_and_term()
    {
        var term = NewTerm();
        var subject = NewSubject();

        var group = SubjectGroup.Create(subject, term, 2, null, 45, Now);

        Assert.Equal(subject.Id, group.SubjectId);
        Assert.Equal(term.Id, group.TermId);
        Assert.Equal(2, group.GroupNumber);
        Assert.Equal(45, group.Capacity);
        Assert.True(group.IsActive);
    }

    [Fact]
    public void Groups_need_an_open_term_an_active_subject_and_sane_numbers()
    {
        var closed = NewTerm();
        closed.Activate(Now);
        closed.Close(Now);
        var inactive = NewSubject();
        inactive.Update("Algorithms", "الخوارزميات", null, 3, SubjectCategory.Regular, false, isActive: false, Now);

        AssertRejected("termId", () => SubjectGroup.Create(NewSubject(), closed, 1, null, 60, Now));
        AssertRejected("subjectId", () => SubjectGroup.Create(inactive, NewTerm(), 1, null, 60, Now));
        AssertRejected("groupNumber", () => SubjectGroup.Create(NewSubject(), NewTerm(), 0, null, 60, Now));
        AssertRejected("capacity", () => SubjectGroup.Create(NewSubject(), NewTerm(), 1, null, 0, Now));
        AssertRejected("capacity", () => SubjectGroup.Create(NewSubject(), NewTerm(), 1, null, 1001, Now));
    }

    [Fact]
    public void Groups_can_be_opened_in_planned_and_active_terms()
    {
        var active = NewTerm();
        active.Activate(Now);

        Assert.NotNull(SubjectGroup.Create(NewSubject(), NewTerm(), 1, null, 60, Now));
        Assert.NotNull(SubjectGroup.Create(NewSubject(), active, 1, null, 60, Now));
    }

    [Fact]
    public void A_teaching_assignment_may_target_one_group_of_the_same_subject_and_term_only()
    {
        var term = NewTerm();
        var subject = NewSubject();
        var ownGroup = NewGroup(term, subject);
        var otherSubjectsGroup = NewGroup(term, NewSubject("CS302"));
        var staff = Guid.CreateVersion7();

        var wholeSubject = TeachingAssignment.Create(staff, subject, term, null, TeachingRole.Lecturer, Guid.CreateVersion7(), Now);
        var oneGroup = TeachingAssignment.Create(staff, subject, term, ownGroup, TeachingRole.Lab, Guid.CreateVersion7(), Now);

        Assert.Null(wholeSubject.SubjectGroupId);
        Assert.Equal(ownGroup.Id, oneGroup.SubjectGroupId);
        Assert.True(oneGroup.IsActive);
        AssertRejected("subjectGroupId", () =>
            TeachingAssignment.Create(staff, subject, term, otherSubjectsGroup, TeachingRole.Lecturer, Guid.CreateVersion7(), Now));
        AssertRejected("subjectGroupId", () =>
            TeachingAssignment.Create(staff, subject, NewTerm(), ownGroup, TeachingRole.Lecturer, Guid.CreateVersion7(), Now));
    }

    [Fact]
    public void Deactivating_a_teaching_assignment()
    {
        var term = NewTerm();
        var assignment = TeachingAssignment.Create(Guid.CreateVersion7(), NewSubject(), term, null, TeachingRole.Lecturer, Guid.CreateVersion7(), Now);

        assignment.Deactivate(Now);

        Assert.False(assignment.IsActive);
    }

    private static void AssertRejected(string field, Action act) =>
        Assert.Equal(field, Assert.ThrowsAny<ArgumentException>(act).ParamName);
}
