using NIMI.UniPortal.Modules.Events.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Events.Application.Manage;

/// <summary>Student Affairs checks a registered member in, from an hour before the start until the end.</summary>
public sealed record CheckInCommand(Guid EventId, Guid UserId) : ICommand<RegistrationDto>;
