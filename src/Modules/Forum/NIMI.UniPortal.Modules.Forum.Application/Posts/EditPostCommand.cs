using NIMI.UniPortal.Modules.Forum.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Forum.Application.Posts;

/// <summary>The author (15-minute window) or Student Affairs (any time) edits a reply.</summary>
public sealed record EditPostCommand(Guid PostId, string? Body, Viewer Caller) : ICommand<PostDto>;
