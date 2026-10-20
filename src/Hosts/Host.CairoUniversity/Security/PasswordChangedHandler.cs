using Microsoft.AspNetCore.Authorization;
using NIMI.UniPortal.SharedKernel.Security;

namespace Host.CairoUniversity.Security;

/// <summary>Satisfied unless the token says <c>must_change_password = true</c>.</summary>
internal sealed class PasswordChangedHandler : AuthorizationHandler<PasswordChangedRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PasswordChangedRequirement requirement)
    {
        var pending = context.User.FindFirst(PortalClaims.MustChangePassword)?.Value;
        if (!string.Equals(pending, "true", StringComparison.OrdinalIgnoreCase))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
