namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Prerequisites;

/// <summary>One prerequisite subject as the API shows it (code, both names).</summary>
public sealed record SubjectPrerequisiteDto(Guid SubjectId, string Code, string Name, string NameAr);
