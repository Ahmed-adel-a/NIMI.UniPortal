using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Registration.RegisterForSubject;
using NIMI.UniPortal.Modules.Scheduling.Application.Catalog.CreateTeachingAssignment;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.Modules.Scheduling.Domain.Catalog;
using NIMI.UniPortal.Modules.Users.Application.Accounts.CreateUser;
using NIMI.UniPortal.Modules.Users.Application.Accounts.SetUserStatus;
using NIMI.UniPortal.Modules.Users.Application.Accounts.TestAccounts;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.Modules.Users.Domain.Accounts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Hosting.DemoData;

/// <summary>
/// Decision NATIONALIDLOGIN, Development only: four accounts that share the owner's national ID and password — a
/// student, a professor, a department head and Student Affairs — so one login tests every role (the sign-in asks
/// which). They join the Computer department (CSE) when it exists, else the first active one: the student registers
/// in the same groups as a demo classmate, the professor teaches the department's groups this term, and the test head
/// takes over the department (the previous head is deactivated). Runs after the demo campus; idempotent.
/// </summary>
public sealed class TestAccountsSeeder(
    TestAccountOptions options,
    ISender sender,
    IUsersApi users,
    ISchedulingApi scheduling,
    IAcademicLifecycle lifecycle,
    TimeProvider clock,
    ILogger<TestAccountsSeeder> logger) : IModuleSeeder
{
    private const string Domain = "@test.cu.edu.eg";

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (!options.Enabled)
            return;
        var ct = cancellationToken;

        var admin = await AccountAsync(UserRole.Admin, "حساب تجريبي - شؤون الطلاب", "admin", null, null, null, ct);
        if (admin is null)
            return;   // already seeded

        var departments = await users.GetDepartmentsAsync(null, ct);
        var department = departments.FirstOrDefault(d => d is { Code: "CSE", IsActive: true }) ?? departments.FirstOrDefault(d => d.IsActive);
        if (department is null)
        {
            logger.LogWarning("Test accounts: no department yet, so only the Student Affairs test account was created.");
            return;
        }

        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var cohort = (today.Month >= 8 ? today.Year : today.Year - 1) - 1;
        var classmate = (await users.GetStudentsAsync(null, department.Id, new PageRequest(1, 1), ct)).Items.FirstOrDefault();

        var student = await AccountAsync(UserRole.Student, "حساب تجريبي - طالب", "student", department.Id, cohort, null, ct);
        var professor = await AccountAsync(UserRole.Professor, "حساب تجريبي - عضو هيئة تدريس", "professor", department.Id, null, AcademicTitle.Lecturer, ct);

        if (await users.GetDepartmentHeadAsync(department.Id, ct) is { } currentHead)
            await sender.Send(new SetUserStatusCommand(currentHead.Id, IsActive: false, admin.Value), ct);
        await AccountAsync(UserRole.DepartmentHead, "حساب تجريبي - رئيس القسم", "head", department.Id, null, AcademicTitle.Professor, ct);

        if (await scheduling.GetCurrentTermAsync(ct) is not { } term)
            return;

        var groups = (await scheduling.GetOfferingsAsync(term.Id, ct))
            .Where(o => o.Subject.DepartmentId == department.Id && o.Group.IsActive)
            .ToList();
        foreach (var offering in groups)
            await TryAsync(() => sender.Send(new CreateTeachingAssignmentCommand(
                professor!.Value, offering.Subject.Id, term.Id, offering.Group.Id, TeachingRole.Lecturer, admin.Value), ct), "teaching assignment");

        if (classmate is not null)
        {
            foreach (var groupId in await lifecycle.GetGroupsForStudentAsync(classmate.StudentId, term.Id, ct))
                await TryAsync(() => sender.Send(new RegisterForSubjectCommand(student!.Value, IsStudent: true, groupId), ct), "registration");
        }

        logger.LogInformation("Test accounts ready in department {Department}: sign in with the national ID and choose a role.", department.Code);
    }

    /// <summary>Creates one account with the owner's password; null when it already exists.</summary>
    private async Task<Guid?> AccountAsync(
        UserRole role, string fullName, string emailName, Guid? departmentId, int? enrollmentYear, AcademicTitle? title, CancellationToken ct)
    {
        try
        {
            var created = await sender.Send(new CreateUserCommand(
                role, fullName, "test." + emailName + Domain, null, departmentId, enrollmentYear, title, options.NationalId, null, null, null), ct);
            await sender.Send(new SetTestAccountPasswordCommand(created.User.Id, options.Password!), ct);
            return created.User.Id;
        }
        catch (ConflictException)
        {
            return null;
        }
    }

    /// <summary>Linking the accounts to classes is best effort: a rule that refuses one link doesn't stop the others.</summary>
    private async Task TryAsync(Func<Task> step, string what)
    {
        try
        {
            await step();
        }
        catch (Exception exception) when (exception is ConflictException or ForbiddenException or FluentValidation.ValidationException)
        {
            logger.LogInformation("Test accounts: skipped a {Step} ({Reason}).", what, exception.Message);
        }
    }
}
