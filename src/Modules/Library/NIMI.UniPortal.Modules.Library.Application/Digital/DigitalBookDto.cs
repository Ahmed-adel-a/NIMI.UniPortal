using NIMI.UniPortal.Modules.Library.Domain.Digital;

namespace NIMI.UniPortal.Modules.Library.Application.Digital;

/// <summary>A digital book as readers and Student Affairs see it; the file is fetched at its download route.</summary>
public sealed record DigitalBookDto(
    Guid Id,
    string Title,
    string Authors,
    string? Description,
    DigitalBookFormat Format,
    string FileName,
    long SizeBytes,
    Guid? FacultyId,
    Guid? DepartmentId,
    Guid? SubjectId,
    int DownloadCount,
    DateTimeOffset CreatedAt)
{
    public static DigitalBookDto From(DigitalBook b) => new(
        b.Id, b.Title, b.Authors, b.Description, b.Format, b.FileName, b.SizeBytes, b.FacultyId, b.DepartmentId, b.SubjectId,
        b.DownloadCount, b.CreatedAt);
}
