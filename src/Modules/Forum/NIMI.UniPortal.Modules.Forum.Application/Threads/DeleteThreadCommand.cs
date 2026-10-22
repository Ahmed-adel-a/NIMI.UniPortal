using NIMI.UniPortal.Modules.Forum.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Forum.Application.Threads;

/// <summary>The author (15-minute window) or Student Affairs (any time) removes a thread (soft delete).</summary>
public sealed record DeleteThreadCommand(Guid ThreadId, Viewer Caller) : ICommand<Unit>;
