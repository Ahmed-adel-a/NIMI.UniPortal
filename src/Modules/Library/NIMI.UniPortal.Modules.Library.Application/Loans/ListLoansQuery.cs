using NIMI.UniPortal.Modules.Library.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Library.Application.Loans;

/// <summary>Student Affairs' loan list: optionally overdue only, open only, or one member's; soonest due first.</summary>
public sealed record ListLoansQuery(bool Overdue, bool? Open, Guid? UserId, PageRequest Paging) : IQuery<PagedResult<LoanDto>>;
