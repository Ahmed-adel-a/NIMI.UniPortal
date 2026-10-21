using NIMI.UniPortal.Modules.Scheduling.Domain.Facilities;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Facilities.ListFacilities;

/// <summary>Lists facilities; a faculty filter also returns the university-shared ones.</summary>
public sealed record ListFacilitiesQuery(
    Guid? FacultyId,
    FacilityType? Type,
    bool IncludeInactive,
    PageRequest Paging) : IQuery<PagedResult<FacilityDto>>;
