using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Audit.Contracts;
using NIMI.UniPortal.Modules.Platform.Contracts;
using NIMI.UniPortal.Modules.Users.Application.Abstractions;
using NIMI.UniPortal.Modules.Users.Domain.Accounts;
using NIMI.UniPortal.Modules.Users.Domain.Organization;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Users.Application.Accounts.CreateUser;

internal sealed class CreateUserHandler(
    IUsersDbContext db,
    IPasswordHasher hasher,
    IStudentCodeAllocator codes,
    IPlatformSettingsApi platform,
    IEventCollector events,
    ICurrentUser currentUser,
    TimeProvider clock) : IRequestHandler<CreateUserCommand, CreatedUserDto>
{
    public async Task<CreatedUserDto> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var email = EmailAddress.Normalize(command.Email, nameof(command.Email));
        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
            throw new ConflictException($"An account with email '{email}' already exists.");

        var temporaryPassword = command.Password is null ? TemporaryPassword.Generate() : null;
        var passwordHash = hasher.Hash(command.Password ?? temporaryPassword!);
        var details = new PersonalDetails(command.NationalId, command.DateOfBirth, command.Gender, command.Phone);
        var now = clock.GetUtcNow();

        Department? department = null;
        Faculty? faculty = null;
        if (command.DepartmentId is { } departmentId)
        {
            department = await db.Departments.SingleOrDefaultAsync(d => d.Id == departmentId, cancellationToken)
                ?? throw ValidationErrors.For(nameof(command.DepartmentId), "Unknown department.");
            faculty = await db.Faculties.SingleAsync(f => f.Id == department.FacultyId, cancellationToken);
        }

        var user = command.Role switch
        {
            UserRole.Student => User.CreateStudent(
                command.FullName, email, passwordHash, department!, command.EnrollmentYear!.Value,
                await NextStudentCode(faculty!, department!, command.EnrollmentYear.Value, cancellationToken),
                details, now),
            UserRole.Professor => User.CreateProfessor(
                command.FullName, email, passwordHash, department!, command.AcademicTitle!.Value, details, now),
            _ => User.CreateAdmin(command.FullName, email, passwordHash, mustChangePassword: true, now),
        };

        db.Users.Add(user);
        events.Add(new AuditableActionPerformed(
            Guid.CreateVersion7(),
            clock.GetUtcNow(),
            currentUser.UserId,
            AuditActions.UserCreated,
            AuditEntities.User,
            user.Id,
            $"{user.Role} account created: {user.FullName}{(user.StudentCode is null ? "" : $" ({user.StudentCode})")}."));

        return new CreatedUserDto(
            new UserDto(
                user.Id, user.FullName, user.Email, user.Role, user.StudentCode, user.AcademicTitle,
                user.DepartmentId, department?.Code, faculty?.Code, user.EnrollmentYear,
                user.IsActive, user.MustChangePassword, user.CreatedAt, user.LastLoginAt),
            temporaryPassword);
    }

    private async Task<string> NextStudentCode(Faculty faculty, Department department, int enrollmentYear, CancellationToken cancellationToken)
    {
        if (!department.IsActive)
            throw ValidationErrors.For("DepartmentId", "The department is deactivated.");   // check before burning a number

        var format = await platform.GetStudentCodeFormatAsync(faculty.Id, cancellationToken);
        var key = StudentCodeFormat.CounterKey(format, faculty.Code, department.Code, enrollmentYear);
        var sequence = await codes.NextAsync(key, cancellationToken);
        return StudentCodeFormat.Render(format, faculty.Code, department.Code, enrollmentYear, sequence);
    }
}
