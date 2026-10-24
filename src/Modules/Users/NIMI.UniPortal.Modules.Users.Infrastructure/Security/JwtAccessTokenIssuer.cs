using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NIMI.UniPortal.Modules.Users.Application.Abstractions;
using NIMI.UniPortal.Modules.Users.Domain.Accounts;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Users.Infrastructure.Security;

/// <summary>HS256 access tokens signed with the deployment's key (<see cref="PortalJwtOptions"/>); the host validates them.</summary>
internal sealed class JwtAccessTokenIssuer(IOptions<PortalJwtOptions> options, TimeProvider clock) : IAccessTokenIssuer
{
    private static readonly JsonWebTokenHandler Handler = new();

    public AccessToken Issue(User user)
    {
        var settings = options.Value;
        var now = clock.GetUtcNow();
        var expiresAt = now + settings.AccessTokenLifetime;

        var claims = new Dictionary<string, object>
        {
            [PortalClaims.UserId] = user.Id.ToString(),
            [PortalClaims.Name] = user.FullName,
            [PortalClaims.Email] = user.Email,
            [PortalClaims.Role] = UserRoles.Name(user.Role),
            [PortalClaims.MustChangePassword] = user.MustChangePassword ? "true" : "false",
        };
        if (user.DepartmentId is { } departmentId)
            claims[PortalClaims.DepartmentId] = departmentId.ToString();

        var token = Handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Claims = claims,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(settings.SigningKeyBytes), SecurityAlgorithms.HmacSha256),
        });

        return new AccessToken(token, expiresAt);
    }
}
