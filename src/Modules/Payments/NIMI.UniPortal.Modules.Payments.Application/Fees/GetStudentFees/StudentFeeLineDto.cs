namespace NIMI.UniPortal.Modules.Payments.Application.Fees.GetStudentFees;

/// <summary>One fee line on the student's own balance page (P5.2).</summary>
public sealed record StudentFeeLineDto(
    Guid FeeItemId,
    string Name,
    string NameAr,
    string Category,
    decimal AmountDue,
    decimal AmountPaid,
    decimal Balance,
    string Status,
    DateOnly DueOn,
    bool IsOverdue);
