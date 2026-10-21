using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Grades.Application.Corrections;

/// <summary>The audit trail of one grade, oldest first (a handful of rows at most, so not paged).</summary>
public sealed record ListCorrectionsQuery(Guid GradeId) : IQuery<IReadOnlyList<GradeCorrectionDto>>;
