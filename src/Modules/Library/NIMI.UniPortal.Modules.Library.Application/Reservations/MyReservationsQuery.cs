using NIMI.UniPortal.Modules.Library.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Library.Application.Reservations;

/// <summary>The caller's reservations, newest first, with their queue position while waiting.</summary>
public sealed record MyReservationsQuery(Viewer Caller, PageRequest Paging) : IQuery<PagedResult<ReservationDto>>;
