using NIMI.UniPortal.Abstractions.Files;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Materials.Application.Abstractions;
using NIMI.UniPortal.Modules.Materials.Application.Common;
using NIMI.UniPortal.Modules.Materials.Domain;
using NIMI.UniPortal.Modules.Notifications.Contracts;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Materials.Application.Upload;

/// <summary>
/// P7.1 — checks who and what before touching the disk: the caller teaches the subject this term (D1, R1), the target
/// group belongs to it, and the file is an allowed type and size (R3). Only then is the file stored under a generated
/// key. The targeted students are notified after the commit (R5).
/// </summary>
internal sealed class UploadMaterialHandler(
    IMaterialsDbContext db,
    CourseAccess access,
    ISchedulingApi scheduling,
    IFileStorage storage,
    IEventCollector events,
    TimeProvider clock) : IRequestHandler<UploadMaterialCommand, MaterialDto>
{
    public async Task<MaterialDto> Handle(UploadMaterialCommand command, CancellationToken cancellationToken)
    {
        if (!command.Caller.IsProfessor)
            throw new ForbiddenException("Only teaching staff upload course materials.");

        var term = await access.ActiveTermAsync(cancellationToken);
        var subject = await scheduling.GetSubjectAsync(command.SubjectId, cancellationToken)
            ?? throw ValidationErrors.For("subjectId", "Unknown subject.");

        if (!await access.TeachesSubjectAsync(command.Caller.UserId, subject.Id, term.Id, cancellationToken))
            throw new ForbiddenException("You do not teach this subject this term.");

        if (command.SubjectGroupId is { } groupId)
        {
            var group = await scheduling.GetGroupAsync(groupId, cancellationToken);
            if (group is null || group.SubjectId != subject.Id || group.TermId != term.Id)
                throw ValidationErrors.For("subjectGroupId", "The group is not a group of this subject in the active term.");
        }

        if (command.Content is null || string.IsNullOrWhiteSpace(command.FileName))
            throw ValidationErrors.For("file", "A file is required.");

        MaterialFiles.EnsureAllowed(command.FileName, command.FileSize);

        var stored = await storage.SaveAsync(
            command.Content, command.FileName, command.ContentType ?? "application/octet-stream", cancellationToken);
        var now = clock.GetUtcNow();
        var material = Material.Create(
            subject.Id,
            term.Id,
            command.SubjectGroupId,
            command.Caller.UserId,
            command.Title,
            command.Description,
            command.WeekNumber,
            stored.Key,
            command.FileName,
            stored.ContentType,
            stored.SizeBytes,
            now);
        db.Materials.Add(material);

        var audience = await access.AudienceAsync(subject.Id, term.Id, command.SubjectGroupId, cancellationToken);
        if (audience.Count > 0)
        {
            events.Add(new NotificationRequested(
                Guid.CreateVersion7(),
                now,
                audience,
                "announcement",
                $"New material in {subject.Code}",
                $"مادة جديدة في {subject.Code}",
                material.Title,
                material.Title,
                $"/materials/{material.Id}"));
        }

        return MaterialDto.From(material, subject.Code);
    }
}
