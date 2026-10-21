using NIMI.UniPortal.Modules.Scheduling.Domain.Facilities;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Facilities;

/// <summary>A facility as the HTTP API shows it; the type serializes as snake_case text.</summary>
public sealed record FacilityDto(
    Guid Id,
    Guid? FacultyId,
    string Name,
    FacilityType Type,
    int Capacity,
    string? Building,
    string? Floor,
    bool IsActive)
{
    public static FacilityDto From(Facility facility) =>
        new(
            facility.Id,
            facility.FacultyId,
            facility.Name,
            facility.Type,
            facility.Capacity,
            facility.Building,
            facility.Floor,
            facility.IsActive);
}
