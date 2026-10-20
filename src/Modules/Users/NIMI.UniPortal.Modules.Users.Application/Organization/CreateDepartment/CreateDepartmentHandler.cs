using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Users.Application.Abstractions;
using NIMI.UniPortal.Modules.Users.Application.Validation;
using NIMI.UniPortal.Modules.Users.Domain.Organization;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Users.Application.Organization.CreateDepartment;

internal sealed class CreateDepartmentHandler(IUsersDbContext db, TimeProvider clock) : IRequestHandler<CreateDepartmentCommand, DepartmentDto>
{
    public async Task<DepartmentDto> Handle(CreateDepartmentCommand command, CancellationToken cancellationToken)
    {
        var faculty = await db.Faculties.SingleOrDefaultAsync(f => f.Id == command.FacultyId, cancellationToken)
            ?? throw ValidationErrors.For(nameof(command.FacultyId), "Unknown faculty.");

        var department = Department.Create(faculty, command.Code, command.Name, command.NameAr, command.Description, clock.GetUtcNow());

        if (await db.Departments.AnyAsync(d => d.Code == department.Code, cancellationToken))
            throw new ConflictException($"A department with code '{department.Code}' already exists.");

        db.Departments.Add(department);
        return new DepartmentDto(
            department.Id, faculty.Id, faculty.Code, department.Code, department.Name, department.NameAr,
            department.Description, department.IsActive);
    }
}
