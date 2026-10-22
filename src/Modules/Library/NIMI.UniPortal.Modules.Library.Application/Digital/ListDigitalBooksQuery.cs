using NIMI.UniPortal.Modules.Library.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Library.Application.Digital;

/// <summary>
/// The digital books the caller may read (Student Affairs: all of them), newest first, searchable by title and
/// author and filterable by subject.
/// </summary>
public sealed record ListDigitalBooksQuery(string? Search, Guid? SubjectId, Viewer Caller, PageRequest Paging)
    : IQuery<PagedResult<DigitalBookDto>>;
