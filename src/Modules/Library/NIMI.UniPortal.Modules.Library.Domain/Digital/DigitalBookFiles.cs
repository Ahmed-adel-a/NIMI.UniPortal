using System.Runtime.CompilerServices;

namespace NIMI.UniPortal.Modules.Library.Domain.Digital;

/// <summary>Decision DIGILIB: PDF and EPUB files up to 100 MB; the format follows the file extension.</summary>
public static class DigitalBookFiles
{
    public const long MaxBytes = 100 * 1024 * 1024;

    // Never inlined: the host recognises a domain rule (400) by the throwing method's assembly.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static DigitalBookFormat Classify(string? fileName, long sizeBytes)
    {
        var format = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant() switch
        {
            ".pdf" => DigitalBookFormat.Pdf,
            ".epub" => DigitalBookFormat.Epub,
            _ => throw new ArgumentException("Digital books are PDF or EPUB files.", "file"),
        };
        if (sizeBytes is < 1 or > MaxBytes)
            throw new ArgumentException("A digital book must be between 1 byte and 100 MB.", "file");
        return format;
    }

    public static string ContentType(DigitalBookFormat format) =>
        format == DigitalBookFormat.Epub ? "application/epub+zip" : "application/pdf";
}
