using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Enrollment;

/// <summary>
/// P4.8: Student Affairs generates a term's enrollments for the university or one department. Each active student
/// takes their level's plan subjects for this term plus the failed subjects they carry; repeating and external
/// students take only their failed subjects (art. 81). Students are spread over each subject's groups (fewest
/// students first, within capacity). Re-running is safe: existing enrollments are kept.
/// </summary>
public sealed record GenerateTermEnrollmentCommand(Guid TermId, Guid? DepartmentId) : ICommand<GenerationReportDto>;
