namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.MilitaryEducation.AssignMilitaryEducation;

/// <summary>The result list of <c>POST /api/admin/military-education/assign</c> (P5.5).</summary>
public sealed record MilitaryEducationAssignmentDto(
    IReadOnlyList<MilitaryEducationAssignmentResultDto> Results);
