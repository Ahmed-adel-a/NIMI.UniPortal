namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Graduation;

/// <summary>GRADREQ — a department's credits to graduate; null while Student Affairs hasn't set it.</summary>
public sealed record GraduationRequirementDto(Guid DepartmentId, int? RequiredCredits, DateTimeOffset? UpdatedAt);
