using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Library.Application.Digital;

/// <summary>Takes a digital book out of the library: readers no longer see or download it (the file is kept).</summary>
public sealed record RemoveDigitalBookCommand(Guid Id) : ICommand<Unit>;
