using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Terms.ListTerms;

public sealed record ListTermsQuery(PageRequest Paging) : IQuery<PagedResult<TermDto>>;
