namespace NIMI.UniPortal.Modules.Forum.Domain;

/// <summary>Trims and checks the forum's texts (titles 1–255, bodies 1–10,000 characters).</summary>
public static class ForumText
{
    public const int MaxTitleLength = 255;
    public const int MaxBodyLength = 10_000;
    public const int EditWindowMinutes = 15;

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
