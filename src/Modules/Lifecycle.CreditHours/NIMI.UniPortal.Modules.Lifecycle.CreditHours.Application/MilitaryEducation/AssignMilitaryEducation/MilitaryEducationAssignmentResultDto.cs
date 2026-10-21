namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.MilitaryEducation.AssignMilitaryEducation;

/// <summary>
/// One per-student result of the admin assignment (P5.5). <see cref="Outcome"/> is <c>assigned</c>,
/// <c>already_registered</c> or <c>not_a_student</c>; the registration id is null only for the last one.
/// </summary>
public sealed record MilitaryEducationAssignmentResultDto(
    Guid StudentId,
    string Outcome,
    Guid? RegistrationId);
