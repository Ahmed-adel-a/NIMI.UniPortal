using NIMI.UniPortal.Modules.Forum.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Forum.Application.Posts;

/// <summary>The author (15-minute window) or Student Affairs (any time) removes a reply (soft delete, placeholder).</summary>
public sealed record DeletePostCommand(Guid PostId, Viewer Caller) : ICommand<Unit>;
