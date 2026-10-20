using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Users.Application.Auth;
using NIMI.UniPortal.Modules.Users.Application.Auth.GetCurrentUser;
using NIMI.UniPortal.Modules.Users.Application.Auth.Login;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Users.Api.Endpoints;

/// <summary>Login + current user (US-X01 partial, US-S21 partial).</summary>
internal static class AuthEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup("/api/auth").WithTags("Auth");

        auth.MapPost("/login", Login).AllowAnonymous().WithName("Login");
        auth.MapGet("/me", Me).RequireAuthorization().WithName("Me");
    }

    private static async Task<Ok<TokenResponse>> Login(LoginCommand command, ISender sender, CancellationToken cancellationToken)
    {
        // The command has already been committed (failed attempts are recorded) before we refuse.
        var token = await sender.Send(command, cancellationToken) ?? throw new AuthenticationFailedException();
        return TypedResults.Ok(token);
    }

    private static async Task<Ok<CurrentUserDto>> Me(ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new GetCurrentUserQuery(user.GetUserId()), cancellationToken));
}
