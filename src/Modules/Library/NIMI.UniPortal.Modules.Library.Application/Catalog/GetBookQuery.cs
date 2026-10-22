using NIMI.UniPortal.Modules.Library.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Library.Application.Catalog;

/// <summary>One book with its copies and the length of its waiting queue.</summary>
public sealed record GetBookQuery(Guid BookId) : IQuery<BookDetailDto>;
