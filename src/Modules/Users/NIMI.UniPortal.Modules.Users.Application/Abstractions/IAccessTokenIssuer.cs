using NIMI.UniPortal.Modules.Users.Domain.Accounts;

namespace NIMI.UniPortal.Modules.Users.Application.Abstractions;

/// <summary>Issues the signed access token (JWT) for a signed-in user.</summary>
public interface IAccessTokenIssuer
{
    AccessToken Issue(User user);
}
