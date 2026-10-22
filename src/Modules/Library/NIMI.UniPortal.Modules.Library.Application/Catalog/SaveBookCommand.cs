using NIMI.UniPortal.Modules.Library.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Library.Application.Catalog;

/// <summary>Student Affairs adds (<see cref="Id"/> null) or edits a catalog entry.</summary>
public sealed record SaveBookCommand(
    Guid? Id,
    string? Isbn,
    string? Title,
    string? Authors,
    string? Publisher,
    int? PublicationYear,
    Guid? FacultyId) : ICommand<BookDetailDto>;
