namespace NIMI.UniPortal.Modules.Payments.Application.Fees.GetStudentFees;

/// <summary>The student's applicable fees and total outstanding balance (P5.2).</summary>
public sealed record StudentFeesDto(IReadOnlyList<StudentFeeLineDto> Fees, decimal TotalBalance);
