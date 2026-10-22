using System.Runtime.CompilerServices;

namespace NIMI.UniPortal.Modules.Forum.Domain;

/// <summary>Trims and checks the forum's texts (titles 1–255, bodies 1–10,000 characters).</summary>
public static class ForumText
{
    public const int MaxTitleLength = 255;
    public const int MaxBodyLength = 10_000;
    public const int EditWindowMinutes = 15;

    // Never inlined: the host recognises a domain rule (400) by the throwing method's assembly, and an inlined helper
    // would report the calling handler's assembly instead (500).
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
}
