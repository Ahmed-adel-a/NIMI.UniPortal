using NIMI.UniPortal.Modules.Library.Domain;

namespace NIMI.UniPortal.Modules.Library.Application.Common;

/// <summary>A book with all its copies and how many members are waiting for it.</summary>
public sealed record BookDetailDto(
    Guid Id,
    string? Isbn,
    string Title,
    string Authors,
    string? Publisher,
    int? PublicationYear,
    Guid? FacultyId,
    IReadOnlyList<CopyDto> Copies,
    int Waiting)
{
    public static BookDetailDto From(Book book, IReadOnlyList<CopyDto> copies, int waiting) =>
        new(book.Id, book.Isbn, book.Title, book.Authors, book.Publisher, book.PublicationYear, book.FacultyId, copies, waiting);
}
