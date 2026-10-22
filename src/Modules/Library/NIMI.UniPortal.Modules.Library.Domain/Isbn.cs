using System.Runtime.CompilerServices;

namespace NIMI.UniPortal.Modules.Library.Domain;

/// <summary>ISBN-10 or ISBN-13 (hyphens and spaces allowed), checked and stored as ISBN-13. Blank means "none".</summary>
public static class Isbn
{
    // Never inlined: the host recognises a domain rule (400) by the throwing method's assembly.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var compact = new string([.. raw.Where(c => c is not ('-' or ' '))]).ToUpperInvariant();
        if (compact.Length == 10 && IsValidIsbn10(compact))
        {
            var body = "978" + compact[..9];
            return body + Isbn13CheckDigit(body);
        }

        if (compact.Length == 13 && compact.All(char.IsAsciiDigit) && Isbn13CheckDigit(compact[..12]) == compact[12])
            return compact;

        throw new ArgumentException("Not a valid ISBN-10 or ISBN-13.", "isbn");
    }

    private static bool IsValidIsbn10(string isbn)
    {
        if (!isbn[..9].All(char.IsAsciiDigit) || !(char.IsAsciiDigit(isbn[9]) || isbn[9] == 'X'))
            return false;

        var sum = 0;
        for (var i = 0; i < 10; i++)
            sum += (10 - i) * (isbn[i] == 'X' ? 10 : isbn[i] - '0');
        return sum % 11 == 0;
    }

    private static char Isbn13CheckDigit(string first12)
    {
        var sum = 0;
        for (var i = 0; i < 12; i++)
            sum += (first12[i] - '0') * (i % 2 == 0 ? 1 : 3);
        return (char)('0' + ((10 - (sum % 10)) % 10));
    }
}
