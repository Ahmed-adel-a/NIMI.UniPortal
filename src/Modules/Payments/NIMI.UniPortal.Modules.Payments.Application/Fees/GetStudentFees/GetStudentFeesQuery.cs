using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Payments.Application.Fees.GetStudentFees;

/// <summary>P5.2 — the signed-in student's applicable fees, optionally narrowed to one term (annual fees stay).</summary>
public sealed record GetStudentFeesQuery(
    Guid StudentId,
    bool IsStudent,
    Guid? TermId) : IQuery<StudentFeesDto>;
