using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;
using NIMI.UniPortal.Modules.Scheduling.Domain.Facilities;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Facilities.CreateFacility;

internal sealed class CreateFacilityHandler(ISchedulingDbContext db, IUsersApi users, TimeProvider clock)
    : IRequestHandler<CreateFacilityCommand, FacilityDto>
{
    public async Task<FacilityDto> Handle(CreateFacilityCommand command, CancellationToken cancellationToken)
    {
        if (command.FacultyId is { } facultyId)
            _ = await users.GetFacultyAsync(facultyId, cancellationToken)
                ?? throw ValidationErrors.For(nameof(command.FacultyId), "Unknown faculty.");

        var facility = Facility.Create(
            command.FacultyId,
            command.Name,
            command.Type,
            command.Capacity,
            command.Building,
            command.Floor,
            clock.GetUtcNow());

        var duplicate = await db.Facilities.AnyAsync(
            f => f.FacultyId == command.FacultyId && f.Name == facility.Name, cancellationToken);
        if (duplicate)
            throw new ConflictException($"A facility named '{facility.Name}' already exists in this scope.");

        db.Facilities.Add(facility);
        return FacilityDto.From(facility);
    }
}
