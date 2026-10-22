using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Users.Application.Auth;
using NIMI.UniPortal.Modules.Users.Application.Auth.ChangePassword;
using NIMI.UniPortal.Modules.Users.Application.Auth.ForgotPassword;
using NIMI.UniPortal.Modules.Users.Application.Auth.GetCurrentUser;
using NIMI.UniPortal.Modules.Users.Application.Auth.Login;
using NIMI.UniPortal.Modules.Users.Application.Auth.Logout;
using NIMI.UniPortal.Modules.Users.Application.Auth.Refresh;
using NIMI.UniPortal.Modules.Users.Application.Auth.ResetPasswordWithToken;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Users.Api.Endpoints;

/// <summary>
/// Login, current user, password change (US-X01 partial, US-S21 partial, US-X26); refresh, logout and password
/// reset by email (decision ACCOUNTX).
/// </summary>
internal static class AuthEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup("/api/auth").WithTags("Auth");

        auth.MapPost("/login", Login).AllowAnonymous().WithName("Login");
        auth.MapPost("/refresh", Refresh).AllowAnonymous().WithName("RefreshSession");
        auth.MapPost("/logout", Logout).AllowAnonymous().WithName("Logout");
        auth.MapPost("/forgot-password", ForgotPassword).AllowAnonymous().WithName("ForgotPassword");
        auth.MapPost("/reset-password", ResetPassword).AllowAnonymous().WithName("ResetPassword");

        // Reachable while a temporary password still has to be changed (everything else answers 403 then).
        auth.MapGet("/me", Me).RequireAuthorization(PortalPolicies.PendingPasswordChange).WithName("Me");
        auth.MapPost("/change-password", ChangePassword).RequireAuthorization(PortalPolicies.PendingPasswordChange).WithName("ChangePassword");
    }

    private static async Task<Ok<TokenResponse>> Login(LoginCommand command, ISender sender, CancellationToken cancellationToken)
    {
        // The command has already been committed (failed attempts are recorded) before we refuse.
        var token = await sender.Send(command, cancellationToken) ?? throw new AuthenticationFailedException();
        return TypedResults.Ok(token);
    }

    private static async Task<Ok<TokenResponse>> Refresh(
        RefreshTokenRequest request, ISender sender, CancellationToken cancellationToken)
    {
        // Committed before refusing: a replayed token's sign-in is revoked even though the call fails.
        var token = await sender.Send(new RefreshSessionCommand(request.RefreshToken), cancellationToken)
            ?? throw new AuthenticationFailedException();
        return TypedResults.Ok(token);
    }

    private static async Task<NoContent> Logout(RefreshTokenRequest request, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new LogoutCommand(request.RefreshToken), cancellationToken);
        return TypedResults.NoContent();
    }

    /// <summary>Always 202, account or not: the answer reveals nothing about which emails exist.</summary>
    private static async Task<Accepted> ForgotPassword(
        ForgotPasswordRequest request, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new ForgotPasswordCommand(request.Email), cancellationToken);
        return TypedResults.Accepted((string?)null);
    }

    private static async Task<NoContent> ResetPassword(
        ResetPasswordRequest request, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new ResetPasswordWithTokenCommand(request.Token, request.NewPassword), cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<CurrentUserDto>> Me(ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new GetCurrentUserQuery(user.GetUserId()), cancellationToken));

    private static async Task<Ok<TokenResponse>> ChangePassword(
        ChangePasswordRequest request, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new ChangePasswordCommand(user.GetUserId(), request.CurrentPassword, request.NewPassword), cancellationToken));
}
