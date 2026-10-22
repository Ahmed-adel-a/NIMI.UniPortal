using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Enrollment;

/// <summary>P4.8: Student Affairs moves a student to another group of the same subject and term (within capacity).</summary>
public sealed record MoveEnrollmentCommand(Guid EnrollmentId, Guid SubjectGroupId) : ICommand<TermEnrollmentDto>;
