using NIMI.UniPortal.Modules.Library.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Library.Application.Catalog;

/// <summary>Student Affairs marks a copy <c>available</c>, <c>lost</c> or <c>withdrawn</c> and moves its shelf.</summary>
public sealed record UpdateCopyCommand(Guid CopyId, string? Status, string? Shelf) : ICommand<CopyDto>;
