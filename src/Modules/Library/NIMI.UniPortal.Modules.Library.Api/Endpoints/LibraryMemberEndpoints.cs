using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Library.Application.Catalog;
using NIMI.UniPortal.Modules.Library.Application.Common;
using NIMI.UniPortal.Modules.Library.Application.Loans;
using NIMI.UniPortal.Modules.Library.Application.Reservations;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Library.Api.Endpoints;

/// <summary>Members search the catalog, see and renew their loans, and reserve books.</summary>
internal static class LibraryMemberEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var library = endpoints.MapGroup("/api/library")
            .WithTags("Library")
            .RequireAuthorization();
        library.MapGet("/books", Search).WithName("SearchLibraryBooks");
        library.MapGet("/books/{id:guid}", Get).WithName("GetLibraryBook");
        library.MapPost("/books/{id:guid}/reservations", Reserve).WithName("ReserveLibraryBook");
        library.MapGet("/loans/mine", MyLoans).WithName("MyLibraryLoans");
        library.MapPost("/loans/{id:guid}/renew", Renew).WithName("RenewLibraryLoan");
        library.MapGet("/reservations/mine", MyReservations).WithName("MyLibraryReservations");
        library.MapDelete("/reservations/{id:guid}", CancelReservation).WithName("CancelLibraryReservation");
    }

    private static async Task<Ok<PagedResult<BookDto>>> Search(
        string? search, Guid? facultyId, int? page, int? pageSize, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new SearchBooksQuery(search, facultyId, new PageRequest(page, pageSize)), cancellationToken));

    private static async Task<Ok<BookDetailDto>> Get(Guid id, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new GetBookQuery(id), cancellationToken));

    private static async Task<Created<ReservationDto>> Reserve(Guid id, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken)
    {
        var reservation = await sender.Send(new ReserveCommand(id, CallerViewer.From(caller)), cancellationToken);
        return TypedResults.Created("/api/library/reservations/mine", reservation);
    }

    private static async Task<Ok<PagedResult<LoanDto>>> MyLoans(
        int? page, int? pageSize, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new MyLoansQuery(CallerViewer.From(caller), new PageRequest(page, pageSize)), cancellationToken));

    private static async Task<Ok<LoanDto>> Renew(Guid id, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new RenewCommand(id, CallerViewer.From(caller)), cancellationToken));

    private static async Task<Ok<PagedResult<ReservationDto>>> MyReservations(
        int? page, int? pageSize, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new MyReservationsQuery(CallerViewer.From(caller), new PageRequest(page, pageSize)), cancellationToken));

    private static async Task<NoContent> CancelReservation(Guid id, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new CancelReservationCommand(id, CallerViewer.From(caller)), cancellationToken);
        return TypedResults.NoContent();
    }
}
