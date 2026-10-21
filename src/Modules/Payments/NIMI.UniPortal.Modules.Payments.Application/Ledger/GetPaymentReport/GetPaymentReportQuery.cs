using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Payments.Application.Ledger.GetPaymentReport;

/// <summary>
/// P5.6 — expected vs collected per fee item, optionally narrowed to a term, a faculty or a department.
/// Expected = amount × active students matching both the report filter and the item's target.
/// </summary>
public sealed record GetPaymentReportQuery(
    Guid? TermId,
    Guid? FacultyId,
    Guid? DepartmentId) : IQuery<PaymentReportDto>;
