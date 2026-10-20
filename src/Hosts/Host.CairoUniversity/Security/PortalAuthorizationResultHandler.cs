using Host.CairoUniversity.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Host.CairoUniversity.Security;

/// <summary>
/// A refusal caused only by a pending password change becomes a 403 the client can act on:
/// <c>{ "code": "password_change_required" }</c>. Every other outcome keeps the default behavior.
/// </summary>
internal sealed class PortalAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        var onlyPasswordPending = authorizeResult.Forbidden
            && authorizeResult.AuthorizationFailure?.FailedRequirements.Any() == true
            && authorizeResult.AuthorizationFailure.FailedRequirements.All(r => r is PasswordChangedRequirement);

        if (onlyPasswordPending)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new ApiError(
                "password_change_required",
                "Change your temporary password first (POST /api/auth/change-password).",
                []));
            return;
        }

        await _default.HandleAsync(next, context, policy, authorizeResult);
    }
}
