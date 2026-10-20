using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Users.Application.Abstractions;
using NIMI.UniPortal.Modules.Users.Application.Validation;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Users.Application.Organization.UpdateDepartment;

internal sealed class UpdateDepartmentHandler(IUsersDbContext db, TimeProvider clock) : IRequestHandler<UpdateDepartmentCommand, DepartmentDto>
{
    public async Task<DepartmentDto> Handle(UpdateDepartmentCommand command, CancellationToken cancellationToken)
    {
        var department = await db.Departments.SingleOrDefaultAsync(d => d.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException("Department not found.");

        var faculty = await db.Faculties.SingleOrDefaultAsync(f => f.Id == command.FacultyId, cancellationToken)
            ?? throw ValidationErrors.For(nameof(command.FacultyId), "Unknown faculty.");

        department.Update(faculty, command.Code, command.Name, command.NameAr, command.Description, command.IsActive, clock.GetUtcNow());

        if (await db.Departments.AnyAsync(d => d.Code == department.Code && d.Id != department.Id, cancellationToken))
            throw new ConflictException($"A department with code '{department.Code}' already exists.");

        return new DepartmentDto(
            department.Id, faculty.Id, faculty.Code, department.Code, department.Name, department.NameAr,
            department.Description, department.IsActive);
    }
}
