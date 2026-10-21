using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Payments.Application.Ledger.ListPayments;

/// <summary>Student Affairs list of payments, filterable by student, fee item and paid date.</summary>
public sealed record ListPaymentsQuery(
    Guid? StudentId,
    Guid? FeeItemId,
    DateOnly? From,
    DateOnly? To,
    PageRequest Paging) : IQuery<PagedResult<AdminPaymentDto>>;
