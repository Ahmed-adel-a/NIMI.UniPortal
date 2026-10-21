namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.MilitaryEducation;

/// <summary>
/// The pending registration created by <c>POST /api/military-education/apply</c> (P5.5). The status is the
/// snake_case wire value used by every lifecycle response.
/// </summary>
public sealed record MilitaryEducationApplicationDto(
    Guid RegistrationId,
    Guid SubjectId,
    string SubjectCode,
    Guid GroupId,
    int GroupNumber,
    string Status);
