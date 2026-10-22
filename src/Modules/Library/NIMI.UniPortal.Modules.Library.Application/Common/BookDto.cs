namespace NIMI.UniPortal.Modules.Library.Application.Common;

/// <summary>A catalog search result with its copy counts (lost and withdrawn copies don't count).</summary>
public sealed record BookDto(
    Guid Id,
    string? Isbn,
    string Title,
    string Authors,
    string? Publisher,
    int? PublicationYear,
    Guid? FacultyId,
    int TotalCopies,
    int AvailableCopies);
