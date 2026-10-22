using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Library.Application.Digital;

/// <summary>Decision DIGILIB: Student Affairs adds a verified e-book (PDF or EPUB, up to 100 MB) for an audience.</summary>
public sealed record AddDigitalBookCommand(
    string? Title,
    string? Authors,
    string? Description,
    Guid? FacultyId,
    Guid? DepartmentId,
    Guid? SubjectId,
    string? FileName,
    long FileSize,
    Stream? Content,
    Guid AdminId) : ICommand<DigitalBookDto>;
