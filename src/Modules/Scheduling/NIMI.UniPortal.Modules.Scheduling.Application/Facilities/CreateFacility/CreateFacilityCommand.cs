using NIMI.UniPortal.Modules.Scheduling.Domain.Facilities;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Facilities.CreateFacility;

public sealed record CreateFacilityCommand(
    Guid? FacultyId,
    string Name,
    FacilityType Type,
    int Capacity,
    string? Building,
    string? Floor) : ICommand<FacilityDto>;
