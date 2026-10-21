using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Registration.GetOfferings;

/// <summary>P4.3 — the active groups of a term (or the active term), with eligibility for this student.</summary>
public sealed record GetOfferingsQuery(
    Guid StudentId,
    bool IsStudent,
    Guid? TermId,
    PageRequest Paging) : IQuery<PagedResult<OfferingDto>>;
