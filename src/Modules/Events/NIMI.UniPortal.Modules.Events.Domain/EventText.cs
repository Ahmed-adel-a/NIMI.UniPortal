using System.Runtime.CompilerServices;

namespace NIMI.UniPortal.Modules.Events.Domain;

/// <summary>Trims and checks event texts.</summary>
public static class EventText
{
    public const int MaxTitleLength = 255;
    public const int MaxDescriptionLength = 5000;
    public const int MaxVenueLength = 255;
    public const int MaxReasonLength = 1000;

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
}
