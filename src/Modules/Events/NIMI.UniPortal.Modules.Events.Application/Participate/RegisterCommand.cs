using NIMI.UniPortal.Modules.Events.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Events.Application.Participate;

/// <summary>A member registers themselves for an event of their audience (R1–R3).</summary>
public sealed record RegisterCommand(Guid EventId, Viewer Caller) : ICommand<RegistrationDto>;
