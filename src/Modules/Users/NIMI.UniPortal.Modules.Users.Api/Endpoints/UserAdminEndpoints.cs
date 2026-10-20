using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Users.Api.Csv;
using NIMI.UniPortal.Modules.Users.Application.Accounts.CreateUser;
using NIMI.UniPortal.Modules.Users.Application.Accounts.ImportStudents;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Users.Api.Endpoints;

/// <summary>Student Affairs account management (US-A01, US-A10).</summary>
internal static class UserAdminEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/admin/users").WithTags("Users").RequireAuthorization(PortalPolicies.Admin);

        admin.MapPost("/", CreateUser).WithName("CreateUser");

        // Bearer-token API (no cookies), so the browser-form antiforgery check doesn't apply.
        admin.MapPost("/bulk", ImportStudents).DisableAntiforgery().WithName("ImportStudents");
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
}
