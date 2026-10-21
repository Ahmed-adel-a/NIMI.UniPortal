using NIMI.UniPortal.Modules.Assignments.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Assignments.Application.Review;

/// <summary>P8.5: approve with a grade (and optional feedback) or reject with a reason — once (R7).</summary>
public sealed record ReviewSubmissionCommand(
    Guid SubmissionId,
    string? Decision,
    decimal? Grade,
    string? Feedback,
    string? RejectionReason,
    Viewer Caller) : ICommand<SubmissionDto>;
