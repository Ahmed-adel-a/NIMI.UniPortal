using NIMI.UniPortal.Modules.Materials.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Materials.Application.Upload;

/// <summary>P7.1: a teaching professor publishes a course file (multipart). <see cref="Content"/> is null when no file was sent.</summary>
public sealed record UploadMaterialCommand(
    Guid SubjectId,
    Guid? SubjectGroupId,
    string Title,
    string? Description,
    int? WeekNumber,
    string? FileName,
    string? ContentType,
    long FileSize,
    Stream? Content,
    Viewer Caller) : ICommand<MaterialDto>;
