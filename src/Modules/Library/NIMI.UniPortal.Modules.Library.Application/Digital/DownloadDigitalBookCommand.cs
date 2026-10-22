using NIMI.UniPortal.Modules.Library.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Library.Application.Digital;

/// <summary>
/// Downloads a digital book the caller may read and counts the download (a command: it changes the count). A book
/// outside the caller's audience is a 404, as if it didn't exist.
/// </summary>
public sealed record DownloadDigitalBookCommand(Guid Id, Viewer Caller) : ICommand<DigitalBookFile>;
