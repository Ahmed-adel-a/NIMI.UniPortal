namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Enrollment;

/// <summary>A student (and subject, when it is about one subject) the generation could not place, and why.</summary>
public sealed record UnplacedDto(Guid StudentId, Guid? SubjectId, string Reason);
