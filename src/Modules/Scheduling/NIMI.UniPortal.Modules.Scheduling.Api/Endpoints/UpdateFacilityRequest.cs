using NIMI.UniPortal.Modules.Scheduling.Domain.Facilities;

namespace NIMI.UniPortal.Modules.Scheduling.Api.Endpoints;

/// <summary>Body of <c>PUT /api/admin/facilities/{id}</c>.</summary>
public sealed record UpdateFacilityRequest(
    Guid? FacultyId,
    string Name,
    FacilityType Type,
    int Capacity,
    string? Building,
    string? Floor,
    bool IsActive);
