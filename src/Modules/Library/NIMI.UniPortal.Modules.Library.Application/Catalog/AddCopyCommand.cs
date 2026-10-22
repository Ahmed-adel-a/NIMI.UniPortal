using NIMI.UniPortal.Modules.Library.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Library.Application.Catalog;

/// <summary>Student Affairs adds a barcoded copy of a book (barcodes are unique).</summary>
public sealed record AddCopyCommand(Guid BookId, string? Barcode, string? Shelf) : ICommand<CopyDto>;
