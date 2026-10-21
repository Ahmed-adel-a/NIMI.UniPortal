// ACCEPTANCE TEST — written by the consultant for phase P04A. Do not modify; report problems instead.
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;

namespace NIMI.UniPortal.Modules.UnitTests.Lifecycle;

/// <summary>Decision D2 — self-registration is auto-approved, so the system enforces every rule (phase §6a order).</summary>
public sealed class RegistrationPolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 20, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Cs = Guid.CreateVersion7();
    private static readonly Guid Civ = Guid.CreateVersion7();

    [Fact]
    public void A_registration_that_breaks_no_rule_passes()
    {
        var context = Valid();

        Assert.Empty(RegistrationPolicy.Check(context));
        RegistrationPolicy.EnsureCanRegister(context);   // does not throw
    }

    [Fact]
    public void A_missing_or_closed_window_means_registration_is_closed()
    {
        var notYetOpen = RegistrationWindow.Create(Guid.CreateVersion7(), Now.AddDays(1), Now.AddDays(10), 0, 21, Now);

        Assert.Equal(["Registration is closed."], RegistrationPolicy.Check(Valid() with { Window = null }));
        Assert.Equal(["Registration is closed."], RegistrationPolicy.Check(Valid() with { Window = notYetOpen }));
    }

    [Fact]
    public void Each_rule_has_its_own_clear_message()
    {
        Assert.Equal(["This group is not offered."], RegistrationPolicy.Check(Valid() with { GroupIsOffered = false }));
        Assert.Equal(["Already registered for this subject."], RegistrationPolicy.Check(Valid() with { AlreadyRegisteredForSubject = true }));
        Assert.Equal(["Registered through the military-education process."], RegistrationPolicy.Check(Valid() with { IsMilitaryEducation = true }));
        Assert.Equal(["This group is reserved for another department."], RegistrationPolicy.Check(Valid() with { TargetDepartmentId = Civ }));
        Assert.Equal(["Missing prerequisite: CS201, MATH101."], RegistrationPolicy.Check(Valid() with { MissingPrerequisiteCodes = ["CS201", "MATH101"] }));
        Assert.Equal(["Exceeds the maximum of 21 credit hours."], RegistrationPolicy.Check(Valid() with { CurrentCredits = 19, SubjectCredits = 3 }));
        Assert.Equal(["Time clash with CS301 group 1, CS305 group 2."], RegistrationPolicy.Check(Valid() with { ClashingGroups = ["CS301 group 1", "CS305 group 2"] }));
    }

    [Fact]
    public void A_group_open_to_everyone_or_to_the_students_own_department_is_fine()
    {
        Assert.Empty(RegistrationPolicy.Check(Valid() with { TargetDepartmentId = null }));
        Assert.Empty(RegistrationPolicy.Check(Valid() with { TargetDepartmentId = Cs }));
    }

    [Fact]
    public void Reaching_the_maximum_exactly_is_allowed()
    {
        Assert.Empty(RegistrationPolicy.Check(Valid() with { CurrentCredits = 18, SubjectCredits = 3 }));
    }

    [Fact]
    public void Every_broken_rule_is_reported_in_order()
    {
        var context = Valid() with
        {
            GroupIsOffered = false,
            TargetDepartmentId = Civ,
            CurrentCredits = 21,
            ClashingGroups = ["CS301 group 1"],
        };

        Assert.Equal(
            [
                "This group is not offered.",
                "This group is reserved for another department.",
                "Exceeds the maximum of 21 credit hours.",
                "Time clash with CS301 group 1.",
            ],
            RegistrationPolicy.Check(context));
    }

    [Fact]
    public void Registering_reports_the_first_broken_rule_on_subjectGroupId()
    {
        var error = Assert.ThrowsAny<ArgumentException>(() =>
            RegistrationPolicy.EnsureCanRegister(Valid() with { TargetDepartmentId = Civ, CurrentCredits = 21 }));

        Assert.Equal("subjectGroupId", error.ParamName);
        Assert.StartsWith("This group is reserved for another department.", error.Message);
    }

    private static RegistrationContext Valid() => new(
        Now: Now,
        Window: RegistrationWindow.Create(Guid.CreateVersion7(), Now.AddDays(-1), Now.AddDays(10), 12, 21, Now.AddDays(-2)),
        GroupIsOffered: true,
        AlreadyRegisteredForSubject: false,
        IsMilitaryEducation: false,
        TargetDepartmentId: null,
        StudentDepartmentId: Cs,
        MissingPrerequisiteCodes: [],
        CurrentCredits: 9,
        SubjectCredits: 3,
        ClashingGroups: []);
}
