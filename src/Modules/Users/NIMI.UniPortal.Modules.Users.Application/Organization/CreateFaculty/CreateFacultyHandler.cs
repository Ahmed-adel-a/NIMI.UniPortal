using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Users.Application.Abstractions;
using NIMI.UniPortal.Modules.Users.Domain.Organization;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Users.Application.Organization.CreateFaculty;

internal sealed class CreateFacultyHandler(IUsersDbContext db, TimeProvider clock) : IRequestHandler<CreateFacultyCommand, FacultyDto>
{
    public async Task<FacultyDto> Handle(CreateFacultyCommand command, CancellationToken cancellationToken)
    {
        var faculty = Faculty.Create(command.Code, command.Name, command.NameAr, clock.GetUtcNow());

        if (await db.Faculties.AnyAsync(f => f.Code == faculty.Code, cancellationToken))
            throw new ConflictException($"A faculty with code '{faculty.Code}' already exists.");

        db.Faculties.Add(faculty);
        return new FacultyDto(faculty.Id, faculty.Code, faculty.Name, faculty.NameAr, faculty.IsActive);
    }
}
