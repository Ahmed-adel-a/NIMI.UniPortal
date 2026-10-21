namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Graduation;

/// <summary>
/// P9.4 — one student's graduation standing. <see cref="Blockers"/>: <c>requirement_not_configured</c> or
/// <c>credits</c>, then <c>required_subject:&lt;code&gt;</c> for every graduation-requirement subject not passed.
/// </summary>
public sealed record GraduationEligibilityDto(
    Guid StudentId,
    string FullName,
    string StudentCode,
    Guid DepartmentId,
    bool Eligible,
    int CreditsEarned,
    int? CreditsRequired,
    IReadOnlyList<string> Blockers);
