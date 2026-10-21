using NIMI.UniPortal.Modules.Scheduling.Domain.Facilities;

namespace NIMI.UniPortal.Modules.Scheduling.Api.Endpoints;

/// <summary>Body of <c>POST /api/admin/facilities</c> (P3.4).</summary>
public sealed record CreateFacilityRequest(
    Guid? FacultyId,
    string Name,
    FacilityType Type,
    int Capacity,
    string? Building,
    string? Floor);
