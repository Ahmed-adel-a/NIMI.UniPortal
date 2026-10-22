using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Library.Application.Digital;

/// <summary>Student Affairs corrects a digital book's details or audience (the file stays).</summary>
public sealed record EditDigitalBookCommand(
    Guid Id, string? Title, string? Authors, string? Description, Guid? FacultyId, Guid? DepartmentId, Guid? SubjectId)
    : ICommand<DigitalBookDto>;
