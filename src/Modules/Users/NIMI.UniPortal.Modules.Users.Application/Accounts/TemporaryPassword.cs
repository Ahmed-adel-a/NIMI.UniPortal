using System.Security.Cryptography;

namespace NIMI.UniPortal.Modules.Users.Application.Accounts;

/// <summary>
/// One-time passwords handed to the admin (decision AUTH: no email in the MVP). 10 characters from an alphabet
/// without look-alikes (0/O, 1/l/I), always with a letter and a digit — so it also satisfies the password policy.
/// </summary>
internal static class TemporaryPassword
{
    private const string Letters = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Digits = "23456789";
    private const int Length = 10;

    public static string Generate()
    {
        var all = Letters + Digits;
        var chars = new char[Length];
        chars[0] = Letters[RandomNumberGenerator.GetInt32(Letters.Length)];
        chars[1] = Digits[RandomNumberGenerator.GetInt32(Digits.Length)];
        for (var i = 2; i < Length; i++)
            chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];

        RandomNumberGenerator.Shuffle(chars.AsSpan());
        return new string(chars);
    }
}
