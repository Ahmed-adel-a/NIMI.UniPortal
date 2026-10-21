using NIMI.UniPortal.Modules.Grades.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Grades.Application.Corrections;

/// <summary>P9 corrections: Student Affairs changes a published grade's score, with a reason (R4).</summary>
public sealed record CorrectGradeCommand(Guid GradeId, decimal Score, string? Reason, Viewer Caller) : ICommand<FinalGradeDto>;
