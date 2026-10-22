using System.Security.Cryptography;
using System.Text;

namespace NIMI.UniPortal.Modules.Users.Application.Auth;

/// <summary>
/// Random, URL-safe secrets for refresh tokens and reset links. Only <see cref="Hash"/> is ever stored, so a leaked
/// database does not leak usable tokens.
/// </summary>
internal static class OpaqueToken
{
    public static (string Value, string Hash) New()
    {
        var value = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return (value, Hash(value));
    }

    public static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
