using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Users.Application.Accounts.CreateUser;
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
    }

    private static async Task<Created<CreatedUserDto>> CreateUser(CreateUserCommand command, ISender sender, CancellationToken cancellationToken)
    {
        var created = await sender.Send(command, cancellationToken);
        return TypedResults.Created($"/api/admin/users/{created.User.Id}", created);
    }
}
