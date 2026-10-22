using System.Runtime.CompilerServices;

namespace NIMI.UniPortal.Modules.Library.Domain;

/// <summary>Trims and checks catalog texts.</summary>
public static class LibraryText
{
    // Never inlined: the host recognises a domain rule (400) by the throwing method's assembly.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string Required(string? value, int maxLength, string paramName)
    {
        var text = value?.Trim();
        if (string.IsNullOrEmpty(text))
            throw new ArgumentException("Required.", paramName);
        if (text.Length > maxLength)
            throw new ArgumentException($"At most {maxLength} characters.", paramName);
        return text;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string? Optional(string? value, int maxLength, string paramName)
    {
        var text = value?.Trim();
        if (string.IsNullOrEmpty(text))
            return null;
        if (text.Length > maxLength)
            throw new ArgumentException($"At most {maxLength} characters.", paramName);
        return text;
    }
}
