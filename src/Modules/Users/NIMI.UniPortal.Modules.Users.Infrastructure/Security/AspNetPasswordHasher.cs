using Microsoft.AspNetCore.Identity;
using NIMI.UniPortal.Modules.Users.Application.Abstractions;

namespace NIMI.UniPortal.Modules.Users.Infrastructure.Security;

/// <summary>ASP.NET Core Identity's hasher (PBKDF2-HMAC-SHA512, 100k iterations, per-hash salt).</summary>
internal sealed class AspNetPasswordHasher : IPasswordHasher
{
    // The hasher ignores the user instance; one stateless instance is enough.
    private static readonly PasswordHasher<object> Hasher = new();
    private static readonly object Unused = new();

    public string Hash(string password) => Hasher.HashPassword(Unused, password);

    public bool Verify(string passwordHash, string password)
    {
        try
        {
            return Hasher.VerifyHashedPassword(Unused, passwordHash, password) is not PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;   // a malformed stored hash never authenticates
        }
    }
}
