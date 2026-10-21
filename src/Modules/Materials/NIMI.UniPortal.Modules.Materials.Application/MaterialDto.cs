using NIMI.UniPortal.Modules.Materials.Domain;

namespace NIMI.UniPortal.Modules.Materials.Application;

/// <summary>A course file as the API shows it (phase §6a). <c>fileType</c> serializes as snake_case text.</summary>
public sealed record MaterialDto(
    Guid Id,
    Guid SubjectId,
    string SubjectCode,
    Guid? SubjectGroupId,
    string Title,
    string? Description,
    int? WeekNumber,
    string FileName,
    string ContentType,
    long SizeBytes,
    MaterialFileType FileType,
    int DownloadCount,
    Guid UploadedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    public static MaterialDto From(Material material, string subjectCode) =>
        new(material.Id,
            material.SubjectId,
            subjectCode,
            material.SubjectGroupId,
            material.Title,
            material.Description,
            material.WeekNumber,
            material.FileName,
            material.ContentType,
            material.SizeBytes,
            material.FileType,
            material.DownloadCount,
            material.UploadedBy,
            material.CreatedAt,
            material.UpdatedAt);
}
