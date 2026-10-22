using NIMI.UniPortal.Modules.Library.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Library.Application.Catalog;

/// <summary>Search by title, author or ISBN (any form), optionally one holding library; by title.</summary>
public sealed record SearchBooksQuery(string? Search, Guid? FacultyId, PageRequest Paging) : IQuery<PagedResult<BookDto>>;
