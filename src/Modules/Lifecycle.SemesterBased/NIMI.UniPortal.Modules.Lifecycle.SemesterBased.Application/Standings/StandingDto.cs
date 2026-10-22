using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Standing;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Standings;

/// <summary>A student's year level (فرقة) and status as Student Affairs sees it.</summary>
public sealed record StandingDto(
    Guid StudentId,
    string FullName,
    string StudentCode,
    Guid DepartmentId,
    int Level,
    int YearsInLevel,
    StandingStatus Status);
