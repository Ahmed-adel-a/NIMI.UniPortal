using NIMI.UniPortal.Modules.Library.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Library.Application.Reservations;

/// <summary>A member leaves the queue; a held copy passes to the next member.</summary>
public sealed record CancelReservationCommand(Guid ReservationId, Viewer Caller) : ICommand<Unit>;
