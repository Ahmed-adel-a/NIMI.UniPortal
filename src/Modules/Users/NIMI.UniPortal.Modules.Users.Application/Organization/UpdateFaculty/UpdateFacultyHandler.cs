using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Users.Application.Abstractions;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Users.Application.Organization.UpdateFaculty;

internal sealed class UpdateFacultyHandler(IUsersDbContext db, TimeProvider clock) : IRequestHandler<UpdateFacultyCommand, FacultyDto>
{
    public async Task<FacultyDto> Handle(UpdateFacultyCommand command, CancellationToken cancellationToken)
    {
        var faculty = await db.Faculties.SingleOrDefaultAsync(f => f.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException("Faculty not found.");

        faculty.Update(command.Code, command.Name, command.NameAr, command.IsActive, clock.GetUtcNow());

        if (await db.Faculties.AnyAsync(f => f.Code == faculty.Code && f.Id != faculty.Id, cancellationToken))
            throw new ConflictException($"A faculty with code '{faculty.Code}' already exists.");

        return new FacultyDto(faculty.Id, faculty.Code, faculty.Name, faculty.NameAr, faculty.IsActive);
    }
}
