using NIMI.UniPortal.Modules.Events.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Events.Application.Participate;

/// <summary>A member gives their seat back, until the event starts.</summary>
public sealed record CancelRegistrationCommand(Guid EventId, Viewer Caller) : ICommand<Unit>;
