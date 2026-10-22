namespace NIMI.UniPortal.Modules.Library.Application.Common;

/// <summary>A loan with the book's title and the copy's barcode.</summary>
public sealed record LoanDto(
    Guid Id,
    Guid BookId,
    string Title,
    Guid CopyId,
    string Barcode,
    Guid UserId,
    DateTimeOffset BorrowedAt,
    DateTimeOffset DueAt,
    DateTimeOffset? ReturnedAt,
    int Renewals,
    bool IsOverdue);
