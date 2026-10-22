using NIMI.UniPortal.Modules.Library.Domain;

namespace NIMI.UniPortal.Modules.Library.Application.Common;

/// <summary>One physical copy; <c>status</c> serializes as snake_case text.</summary>
public sealed record CopyDto(Guid Id, Guid BookId, string Barcode, string? Shelf, CopyStatus Status)
{
    public static CopyDto From(BookCopy copy) => new(copy.Id, copy.BookId, copy.Barcode, copy.Shelf, copy.Status);
}
