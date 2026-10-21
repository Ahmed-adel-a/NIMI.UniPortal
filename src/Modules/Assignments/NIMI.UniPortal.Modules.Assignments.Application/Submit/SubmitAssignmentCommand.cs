using NIMI.UniPortal.Modules.Assignments.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Assignments.Application.Submit;

/// <summary>P8.3: a registered student hands in one file before the deadline. <see cref="Content"/> is null when no file was sent.</summary>
public sealed record SubmitAssignmentCommand(
    Guid AssignmentId,
    string? FileName,
    string? ContentType,
    long FileSize,
    Stream? Content,
    Viewer Caller) : ICommand<SubmissionReceiptDto>;
