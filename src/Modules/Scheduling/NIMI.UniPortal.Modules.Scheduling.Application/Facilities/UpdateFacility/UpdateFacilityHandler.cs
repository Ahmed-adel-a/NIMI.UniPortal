using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Facilities.UpdateFacility;

internal sealed class UpdateFacilityHandler(ISchedulingDbContext db, IUsersApi users, TimeProvider clock)
    : IRequestHandler<UpdateFacilityCommand, FacilityDto>
{
    public async Task<FacilityDto> Handle(UpdateFacilityCommand command, CancellationToken cancellationToken)
    {
        var facility = await db.Facilities.FirstOrDefaultAsync(f => f.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException($"No facility with id '{command.Id}'.");

        if (command.FacultyId is { } facultyId)
            _ = await users.GetFacultyAsync(facultyId, cancellationToken)
                ?? throw ValidationErrors.For(nameof(command.FacultyId), "Unknown faculty.");

        facility.Update(
            command.FacultyId,
            command.Name,
            command.Type,
            command.Capacity,
            command.Building,
            command.Floor,
            command.IsActive,
            clock.GetUtcNow());

        var duplicate = await db.Facilities.AnyAsync(
            f => f.Id != facility.Id && f.FacultyId == command.FacultyId && f.Name == facility.Name,
            cancellationToken);
        if (duplicate)
            throw new ConflictException($"A facility named '{facility.Name}' already exists in this scope.");

        return FacilityDto.From(facility);
    }
}
