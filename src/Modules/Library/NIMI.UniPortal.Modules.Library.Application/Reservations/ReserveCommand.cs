using NIMI.UniPortal.Modules.Library.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Library.Application.Reservations;

/// <summary>A member queues for a book that has no copy on the shelf.</summary>
public sealed record ReserveCommand(Guid BookId, Viewer Caller) : ICommand<ReservationDto>;
