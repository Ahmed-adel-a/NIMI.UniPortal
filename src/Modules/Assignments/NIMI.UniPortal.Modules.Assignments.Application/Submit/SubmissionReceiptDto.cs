using NIMI.UniPortal.Modules.Assignments.Domain;

namespace NIMI.UniPortal.Modules.Assignments.Application.Submit;

/// <summary>What the student gets back right after handing in: the receipt number is their proof (R6).</summary>
public sealed record SubmissionReceiptDto(Guid Id, string ReceiptNumber, DateTimeOffset SubmittedAt, SubmissionStatus Status);
