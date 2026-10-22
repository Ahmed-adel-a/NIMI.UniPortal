using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Library.Application.Catalog;
using NIMI.UniPortal.Modules.Library.Application.Common;
using NIMI.UniPortal.Modules.Library.Application.Loans;
using NIMI.UniPortal.Modules.Library.Application.Policies;
using NIMI.UniPortal.Modules.Library.Application.Reminders;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Library.Api.Endpoints;

/// <summary>Student Affairs runs the library (LIBROLE): catalog, lending desk, loan rules, reminders.</summary>
internal static class LibraryAdminEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/admin/library")
            .WithTags("Library")
            .RequireAuthorization(PortalPolicies.Admin);
        admin.MapPost("/books", AddBook).WithName("AddLibraryBook");
        admin.MapPut("/books/{id:guid}", UpdateBook).WithName("UpdateLibraryBook");
        admin.MapPost("/books/{id:guid}/copies", AddCopy).WithName("AddLibraryCopy");
        admin.MapPut("/copies/{id:guid}", UpdateCopy).WithName("UpdateLibraryCopy");
        admin.MapPost("/loans", Lend).WithName("LendLibraryCopy");
        admin.MapPost("/loans/{id:guid}/return", Return).WithName("ReturnLibraryLoan");
        admin.MapGet("/loans", Loans).WithName("ListLibraryLoans");
        admin.MapGet("/policies", Policies).WithName("ListLibraryPolicies");
        admin.MapPut("/policies/{role}", UpdatePolicy).WithName("UpdateLibraryPolicy");
        admin.MapPost("/reminders/run", RunReminders).WithName("RunLibraryReminders");
    }

    private static async Task<Created<BookDetailDto>> AddBook(BookRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var book = await sender.Send(
            new SaveBookCommand(null, request.Isbn, request.Title, request.Authors, request.Publisher, request.PublicationYear, request.FacultyId),
            cancellationToken);
        return TypedResults.Created($"/api/library/books/{book.Id}", book);
    }

    private static async Task<Ok<BookDetailDto>> UpdateBook(Guid id, BookRequest request, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new SaveBookCommand(id, request.Isbn, request.Title, request.Authors, request.Publisher, request.PublicationYear, request.FacultyId),
            cancellationToken));

    private static async Task<Created<CopyDto>> AddCopy(Guid id, CopyRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var copy = await sender.Send(new AddCopyCommand(id, request.Barcode, request.Shelf), cancellationToken);
        return TypedResults.Created($"/api/library/books/{id}", copy);
    }

    private static async Task<Ok<CopyDto>> UpdateCopy(Guid id, CopyUpdateRequest request, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new UpdateCopyCommand(id, request.Status, request.Shelf), cancellationToken));

    private static async Task<Created<LoanDto>> Lend(LendRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var loan = await sender.Send(new LendCommand(request.Barcode, request.UserId), cancellationToken);
        return TypedResults.Created($"/api/admin/library/loans/{loan.Id}", loan);
    }

    private static async Task<Ok<LoanDto>> Return(Guid id, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new ReturnCommand(id), cancellationToken));

    private static async Task<Ok<PagedResult<LoanDto>>> Loans(
        bool? overdue, bool? open, Guid? userId, int? page, int? pageSize, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new ListLoansQuery(overdue ?? false, open, userId, new PageRequest(page, pageSize)), cancellationToken));

    private static async Task<Ok<IReadOnlyList<PolicyDto>>> Policies(ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new ListPoliciesQuery(), cancellationToken));

    private static async Task<Ok<PolicyDto>> UpdatePolicy(string role, PolicyRequest request, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new UpdatePolicyCommand(role, request.LoanDays, request.MaxOpenLoans, request.MaxRenewals, request.HoldDays), cancellationToken));

    private static async Task<Ok<ReminderRunDto>> RunReminders(ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new RunRemindersCommand(), cancellationToken));
}
