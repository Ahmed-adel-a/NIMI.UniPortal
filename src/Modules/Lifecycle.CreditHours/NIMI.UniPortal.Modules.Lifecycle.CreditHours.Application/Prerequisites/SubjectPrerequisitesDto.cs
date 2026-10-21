namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Prerequisites;

/// <summary>A subject's full prerequisite list, ordered by code (P4.2).</summary>
public sealed record SubjectPrerequisitesDto(Guid SubjectId, IReadOnlyList<SubjectPrerequisiteDto> Prerequisites);
