using NIMI.UniPortal.Modules.Forum.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Forum.Application.Posts;

/// <summary>Reply to a visible, unlocked thread.</summary>
public sealed record ReplyCommand(Guid ThreadId, string? Body, Viewer Caller) : ICommand<PostDto>;
