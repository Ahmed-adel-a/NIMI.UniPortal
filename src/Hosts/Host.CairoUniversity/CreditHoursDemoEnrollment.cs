using NIMI.UniPortal.Hosting.DemoData;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Graduation;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Prerequisites.SetSubjectPrerequisites;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Registration.RegisterForSubject;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.RegistrationWindows.CreateRegistrationWindow;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Host.CairoUniversity;

/// <summary>
/// Cairo's lifecycle (CreditHours) half of the demo campus: prerequisites, a registration window that is still open
/// (so the frontend can register and drop too), the students' self-registrations and a graduation requirement.
/// </summary>
public sealed class CreditHoursDemoEnrollment(ISender sender, TimeProvider clock) : IDemoEnrollment
{
    public async Task EnrollAsync(DemoCampus campus, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        foreach (var subject in campus.Prerequisites.GroupBy(p => p.SubjectId))
            await sender.Send(new SetSubjectPrerequisitesCommand(subject.Key, [.. subject.Select(p => p.PrerequisiteId)]), cancellationToken);

        await sender.Send(new CreateRegistrationWindowCommand(campus.TermId, now.AddDays(-7), now.AddDays(30), 6, 18), cancellationToken);
        foreach (var (studentId, groupId) in campus.Placements)
            await sender.Send(new RegisterForSubjectCommand(studentId, IsStudent: true, groupId), cancellationToken);

        await sender.Send(new SetGraduationRequirementCommand(campus.ComputerDepartmentId, 160, campus.AdminId), cancellationToken);
    }
}
