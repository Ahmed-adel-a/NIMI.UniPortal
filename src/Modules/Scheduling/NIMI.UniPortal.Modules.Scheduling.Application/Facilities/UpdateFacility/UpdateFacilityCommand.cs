using NIMI.UniPortal.Modules.Scheduling.Domain.Facilities;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Facilities.UpdateFacility;

public sealed record UpdateFacilityCommand(
    Guid Id,
    Guid? FacultyId,
    string Name,
    FacilityType Type,
    int Capacity,
    string? Building,
    string? Floor,
    bool IsActive) : ICommand<FacilityDto>;
