using NIMI.UniPortal.Modules.Events.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Events.Application.Participate;

/// <summary>One event the caller may see (published or cancelled, their audience; Student Affairs: any).</summary>
public sealed record GetEventQuery(Guid EventId, Viewer Caller) : IQuery<EventDto>;
