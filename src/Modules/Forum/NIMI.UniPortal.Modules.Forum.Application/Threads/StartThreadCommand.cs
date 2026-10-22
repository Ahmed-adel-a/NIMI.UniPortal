using NIMI.UniPortal.Modules.Forum.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Forum.Application.Threads;

/// <summary>Start a thread in a visible category, optionally about one subject.</summary>
public sealed record StartThreadCommand(Guid CategoryId, string? Title, string? Body, Guid? SubjectId, Viewer Caller)
    : ICommand<ThreadDto>;
