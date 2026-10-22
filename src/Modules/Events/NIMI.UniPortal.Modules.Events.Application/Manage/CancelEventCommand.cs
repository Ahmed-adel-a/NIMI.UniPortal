using NIMI.UniPortal.Modules.Events.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Events.Application.Manage;

/// <summary>Student Affairs cancels an event with a reason; registrants are notified (R4).</summary>
public sealed record CancelEventCommand(Guid EventId, string? Reason, Guid AdminId) : ICommand<EventDto>;
