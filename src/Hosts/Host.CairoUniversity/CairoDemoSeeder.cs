using NIMI.UniPortal.Hosting.DemoData;
using NIMI.UniPortal.Modules.Events.Application.Manage;
using NIMI.UniPortal.Modules.Events.Application.Participate;
using NIMI.UniPortal.Modules.Forum.Application.Categories;
using NIMI.UniPortal.Modules.Forum.Application.Posts;
using NIMI.UniPortal.Modules.Forum.Application.Threads;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Persistence;
using EventViewer = NIMI.UniPortal.Modules.Events.Application.Common.Viewer;
using ForumViewer = NIMI.UniPortal.Modules.Forum.Application.Common.Viewer;

namespace NIMI.UniPortal.Host.CairoUniversity;

/// <summary>
/// The premium modules Cairo bought, on top of the shared demo campus: forum categories with a conversation, and a
/// published event with a registration. Runs only when the shared seeder built the campus in this run.
/// </summary>
public sealed class CairoDemoSeeder(ISender sender, TimeProvider clock, DemoCampus campus) : IModuleSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (!campus.Seeded)
            return;

        var ct = cancellationToken;
        var admin = new ForumViewer(campus.AdminId, IsAdmin: true, IsProfessor: false, IsStudent: false);
        var student = new ForumViewer(campus.StudentId, IsAdmin: false, IsProfessor: false, IsStudent: true);
        var professor = new ForumViewer(campus.ProfessorId, IsAdmin: false, IsProfessor: true, IsStudent: false);

        var general = await sender.Send(new SaveCategoryCommand(null, "General", "عام", null, 1, true), ct);
        var engineering = await sender.Send(new SaveCategoryCommand(
            null, "Engineering", "الهندسة", campus.EngineeringFacultyId, 2, true), ct);

        await sender.Send(new StartThreadCommand(general.Id, "أهلاً بكم في منتدى الطلاب",
            "Ask questions, share notes and help each other. Please keep it respectful.", null, admin), ct);
        var question = await sender.Send(new StartThreadCommand(engineering.Id, "CSE101: how should I practise loops?",
            "Any recommended exercises before the first assignment?", campus.ProgrammingSubjectId, student), ct);
        await sender.Send(new ReplyCommand(question.Id,
            "Start with the lab sheet from Lecture 1, then try printing number patterns. Bring questions to office hours.", professor), ct);

        var startsAt = new DateTimeOffset(DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime).AddDays(10), new TimeOnly(8, 0), TimeSpan.Zero);
        var fair = await sender.Send(new SaveEventCommand(null, "Career Fair 2026",
            "Meet engineering and finance employers, bring your CV.", null, "Main Auditorium",
            startsAt, startsAt.AddHours(6), 300, startsAt.AddDays(-2), campus.AdminId), ct);
        await sender.Send(new PublishEventCommand(fair.Id, campus.AdminId), ct);
        await sender.Send(new RegisterCommand(fair.Id, new EventViewer(campus.StudentId, IsAdmin: false, IsProfessor: false, IsStudent: true)), ct);
    }
}
