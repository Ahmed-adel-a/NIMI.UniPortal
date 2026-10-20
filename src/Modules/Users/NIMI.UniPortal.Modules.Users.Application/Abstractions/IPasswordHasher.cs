namespace NIMI.UniPortal.Modules.Users.Application.Abstractions;

/// <summary>Password hashing (implemented in Infrastructure with ASP.NET Core's PBKDF2 hasher).</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string passwordHash, string password);
}
