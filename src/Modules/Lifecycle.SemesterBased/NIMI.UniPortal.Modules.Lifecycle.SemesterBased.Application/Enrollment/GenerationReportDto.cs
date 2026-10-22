namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Enrollment;

/// <summary>
/// What one generation run did: new enrollments, ones that already existed (a re-run changes nothing), students
/// placed in level 1 because they had no standing yet, and everything it could not place.
/// </summary>
public sealed record GenerationReportDto(int Enrolled, int AlreadyEnrolled, int NewStandings, IReadOnlyList<UnplacedDto> Unplaced);
