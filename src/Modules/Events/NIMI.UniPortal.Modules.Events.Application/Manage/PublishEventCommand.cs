using NIMI.UniPortal.Modules.Events.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Events.Application.Manage;

/// <summary>Student Affairs publishes a draft to its audience (no notification — R5).</summary>
public sealed record PublishEventCommand(Guid EventId, Guid AdminId) : ICommand<EventDto>;
