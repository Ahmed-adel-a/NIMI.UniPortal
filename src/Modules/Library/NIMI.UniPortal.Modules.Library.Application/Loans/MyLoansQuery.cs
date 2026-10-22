using NIMI.UniPortal.Modules.Library.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Library.Application.Loans;

/// <summary>The caller's loans: open ones first, then returned ones; each by due date.</summary>
public sealed record MyLoansQuery(Viewer Caller, PageRequest Paging) : IQuery<PagedResult<LoanDto>>;
