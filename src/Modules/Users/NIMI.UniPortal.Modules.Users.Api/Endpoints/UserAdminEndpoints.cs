using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Users.Api.Csv;
using NIMI.UniPortal.Modules.Users.Application.Accounts;
using NIMI.UniPortal.Modules.Users.Application.Accounts.CreateUser;
using NIMI.UniPortal.Modules.Users.Application.Accounts.ImportStudents;
using NIMI.UniPortal.Modules.Users.Application.Accounts.ListUsers;
using NIMI.UniPortal.Modules.Users.Application.Accounts.ResetPassword;
using NIMI.UniPortal.Modules.Users.Application.Accounts.SetUserStatus;
using NIMI.UniPortal.Modules.Users.Domain.Accounts;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Users.Api.Endpoints;

/// <summary>Student Affairs account management (US-A01, US-A10).</summary>
internal static class UserAdminEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/admin/users").WithTags("Users").RequireAuthorization(PortalPolicies.Admin);

        admin.MapGet("/", ListUsers).WithName("ListUsers");
        admin.MapPost("/", CreateUser).WithName("CreateUser");

        // Bearer-token API (no cookies), so the browser-form antiforgery check doesn't apply.
        admin.MapPost("/bulk", ImportStudents).DisableAntiforgery().WithName("ImportStudents");

        admin.MapPatch("/{id:guid}/status", SetStatus).WithName("SetUserStatus");
        admin.MapPost("/{id:guid}/reset-password", ResetPassword).WithName("ResetUserPassword");
    }

    /// <param name="role">student / professor / department_head / admin — same spelling as in JSON bodies (case-insensitive).</param>
    private static async Task<Ok<PagedResult<UserDto>>> ListUsers(
        ISender sender, string? role, Guid? facultyId, Guid? departmentId, bool? isActive, string? search,
        int? page, int? pageSize, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new ListUsersQuery(ParseRole(role), facultyId, departmentId, isActive, search, new PageRequest(page, pageSize)), cancellationToken));

    /// <summary>
    /// Query strings don't go through the JSON enum converter (ASP.NET's default enum binding is case-sensitive),
    /// so "?role=student" is parsed here, the same way bodies are.
    /// </summary>
    private static UserRole? ParseRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return null;
        if (UserRoles.TryParse(role, out var parsed))
            return parsed;
        throw new FluentValidation.ValidationException([new FluentValidation.Results.ValidationFailure("role", "Use student, professor, department_head or admin.")]);
    }

    private static async Task<Created<CreatedUserDto>> CreateUser(CreateUserCommand command, ISender sender, CancellationToken cancellationToken)
    {
        var created = await sender.Send(command, cancellationToken);
        return TypedResults.Created($"/api/admin/users/{created.User.Id}", created);
    }

    /// <summary>multipart/form-data with one CSV in the field <c>file</c>. Always 200 with the per-row report.</summary>
    private static async Task<Ok<ImportStudentsReport>> ImportStudents(IFormFile? file, ISender sender, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            throw StudentCsvReader.FileError("Attach the CSV in the form field 'file'.");
        if (file.Length > StudentCsvReader.MaxBytes)
            throw StudentCsvReader.FileError("The file is larger than 5 MB.");

        await using var stream = file.OpenReadStream();
        var rows = await StudentCsvReader.ReadAsync(stream, cancellationToken);
        return TypedResults.Ok(await sender.Send(new ImportStudentsCommand(rows), cancellationToken));
    }

    private static async Task<NoContent> SetStatus(
        Guid id, SetUserStatusRequest request, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new SetUserStatusCommand(id, request.IsActive, caller.GetUserId()), cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<TemporaryPasswordDto>> ResetPassword(Guid id, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new ResetPasswordCommand(id), cancellationToken));
}
