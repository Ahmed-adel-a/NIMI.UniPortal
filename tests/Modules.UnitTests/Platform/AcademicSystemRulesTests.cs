// ACCEPTANCE TEST — written by the consultant for decision LIFECYCLEDEPT. Do not modify; report problems instead.
using NIMI.UniPortal.Modules.Platform.Domain.AcademicSystems;

namespace NIMI.UniPortal.Modules.UnitTests.Platform;

/// <summary>A department's academic system: its own rule, else its faculty's, else the university's, else the default.</summary>
public sealed class AcademicSystemRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Eng = Guid.CreateVersion7();
    private static readonly Guid Com = Guid.CreateVersion7();
    private static readonly Guid Cs = Guid.CreateVersion7();
    private static readonly Guid Ee = Guid.CreateVersion7();

    private static (AcademicSystem, AcademicSystemSource) Resolve(Guid? faculty, Guid? department, params AcademicSystemRule[] rules) =>
        AcademicSystemRules.Resolve(rules, faculty, department, AcademicSystem.CreditHours);

    [Fact]
    public void Without_any_rule_the_default_applies()
    {
        Assert.Equal((AcademicSystem.CreditHours, AcademicSystemSource.Default), Resolve(Eng, Cs));
    }

    [Fact]
    public void The_university_rule_applies_everywhere_until_overridden()
    {
        var university = AcademicSystemRule.ForUniversity(AcademicSystem.YearGroups, Now);

        Assert.Equal((AcademicSystem.YearGroups, AcademicSystemSource.University), Resolve(Eng, Cs, university));
        Assert.Equal((AcademicSystem.YearGroups, AcademicSystemSource.University), Resolve(null, null, university));
    }

    [Fact]
    public void A_faculty_rule_wins_over_the_university_and_a_department_rule_wins_over_its_faculty()
    {
        AcademicSystemRule[] rules =
        [
            AcademicSystemRule.ForUniversity(AcademicSystem.CreditHours, Now),
            AcademicSystemRule.ForFaculty(Com, AcademicSystem.YearGroups, Now),
            AcademicSystemRule.ForDepartment(Ee, AcademicSystem.YearGroups, Now),
        ];

        Assert.Equal((AcademicSystem.CreditHours, AcademicSystemSource.University), Resolve(Eng, Cs, rules));
        Assert.Equal((AcademicSystem.YearGroups, AcademicSystemSource.Department), Resolve(Eng, Ee, rules));
        Assert.Equal((AcademicSystem.YearGroups, AcademicSystemSource.Faculty), Resolve(Com, Guid.CreateVersion7(), rules));
    }

    [Fact]
    public void Another_facultys_rule_does_not_apply()
    {
        var commerce = AcademicSystemRule.ForFaculty(Com, AcademicSystem.YearGroups, Now);

        Assert.Equal((AcademicSystem.CreditHours, AcademicSystemSource.Default), Resolve(Eng, Cs, commerce));
    }
}
