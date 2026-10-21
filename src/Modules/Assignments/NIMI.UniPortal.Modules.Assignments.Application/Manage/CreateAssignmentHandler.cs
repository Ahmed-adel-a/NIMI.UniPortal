using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Assignments.Application.Abstractions;
using NIMI.UniPortal.Modules.Assignments.Application.Common;
using NIMI.UniPortal.Modules.Assignments.Domain;
using NIMI.UniPortal.Modules.Notifications.Contracts;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Assignments.Application.Manage;

/// <summary>P8.1 — only staff teaching the subject this term (R1); the targeted students are notified after the commit.</summary>
internal sealed class CreateAssignmentHandler(
    IAssignmentsDbContext db,
    CourseAccess course,
    ISchedulingApi scheduling,
    AssignmentPresenter presenter,
    IEventCollector events,
    TimeProvider clock) : IRequestHandler<CreateAssignmentCommand, AssignmentDto>
{
    public async Task<AssignmentDto> Handle(CreateAssignmentCommand command, CancellationToken cancellationToken)
    {
        if (!command.Caller.IsProfessor)
            throw new ForbiddenException("Only teaching staff set assignments.");

        var term = await course.ActiveTermAsync(cancellationToken);
        var subject = await scheduling.GetSubjectAsync(command.SubjectId, cancellationToken)
            ?? throw ValidationErrors.For("subjectId", "Unknown subject.");

        if (!await course.TeachesSubjectAsync(command.Caller.UserId, subject.Id, term.Id, cancellationToken))
            throw new ForbiddenException("You do not teach this subject this term.");

        if (command.SubjectGroupId is { } groupId)
        {
            var group = await scheduling.GetGroupAsync(groupId, cancellationToken);
            if (group is null || group.SubjectId != subject.Id || group.TermId != term.Id)
                throw ValidationErrors.For("subjectGroupId", "The group is not a group of this subject in the active term.");
        }

        var now = clock.GetUtcNow();
        var assignment = Assignment.Create(
            subject.Id,
            term.Id,
            command.SubjectGroupId,
            command.Caller.UserId,
            command.Title,
            command.Instructions,
            command.DueAt,
            command.MaxGrade,
            command.AllowedExtensions,
            command.MaxFileMb,
            now);
        db.Assignments.Add(assignment);

        var audience = await course.AudienceAsync(subject.Id, term.Id, command.SubjectGroupId, cancellationToken);
        if (audience.Count > 0)
        {
            events.Add(new NotificationRequested(
                Guid.CreateVersion7(),
                now,
                audience,
                "assignment",
                Clip($"New assignment in {subject.Code}: {assignment.Title}"),
                Clip($"تكليف جديد في {subject.Code}: {assignment.Title}"),
                $"Due {assignment.DueAt:yyyy-MM-dd HH:mm} UTC.",
                $"آخر موعد للتسليم {assignment.DueAt:yyyy-MM-dd HH:mm} بتوقيت UTC.",
                $"/assignments/{assignment.Id}"));
        }

        return await presenter.PresentAsync(assignment, command.Caller, staffView: true, cancellationToken);
    }

    private static string Clip(string text) => text.Length <= 200 ? text : text[..197] + "...";
}
