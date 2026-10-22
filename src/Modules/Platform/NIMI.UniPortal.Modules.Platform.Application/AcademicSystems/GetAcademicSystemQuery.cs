using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Platform.Application.AcademicSystems;

/// <summary>The academic system in force for a department, or the university value when no department is given.</summary>
public sealed record GetAcademicSystemQuery(Guid? DepartmentId) : IQuery<AcademicSystemDto>;
