using System.Security.Claims;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Hosting.Security;

/// <summary>Reads the caller's id from the current HTTP request (real tokens and the test scheme alike).</summary>
internal sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            var principal = accessor.HttpContext?.User;
            var value = principal?.FindFirst(PortalClaims.UserId)?.Value ?? principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }
}
