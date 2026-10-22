using System.Globalization;
using System.Text;

namespace NIMI.UniPortal.SharedKernel.Documents;

/// <summary>
/// Writes a minimal PDF 1.4 file: a catalog, the A4 pages with their content streams, and the two standard fonts.
/// No compression and no embedded fonts, so the output is small, deterministic and readable by every viewer.
/// </summary>
internal static class PdfFile
{
    public const double PageWidth = 595;
    public const double PageHeight = 842;

    public static byte[] Write(IReadOnlyList<PdfPageContent> pages, string title)
    {
        // Objects: 1 catalog, 2 page tree, 3 Helvetica, 4 Helvetica-Bold, 5 info, then (page, content) per page.
        var objects = new List<byte[]>
        {
            Ascii("<< /Type /Catalog /Pages 2 0 R >>"),
            Ascii($"<< /Type /Pages /Kids [{string.Join(' ', pages.Select((_, i) => $"{6 + (i * 2)} 0 R"))}] /Count {pages.Count} >>"),
            Ascii("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"),
            Ascii("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>"),
            Encoding.Latin1.GetBytes($"<< /Title ({TitleText(title)}) /Producer (NIMI UniPortal) >>"),
        };

        for (var i = 0; i < pages.Count; i++)
        {
            var contentId = 7 + (i * 2);
            objects.Add(Ascii(
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageWidth} {PageHeight}] " +
                $"/Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {contentId} 0 R >>"));
            var stream = pages[i].ToBytes();
            objects.Add([.. Ascii($"<< /Length {stream.Length} >>\nstream\n"), .. stream, .. Ascii("\nendstream")]);
        }

        using var output = new MemoryStream();
        Write(output, Ascii("%PDF-1.4\n%âãÏÓ\n"));
        var offsets = new List<long>(objects.Count);
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(output.Position);
            Write(output, Ascii($"{i + 1} 0 obj\n"));
            Write(output, objects[i]);
            Write(output, Ascii("\nendobj\n"));
        }

        var xref = output.Position;
        var table = new StringBuilder($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets)
            table.Append(offset.ToString("D10", CultureInfo.InvariantCulture)).Append(" 00000 n \n");
        table.Append(CultureInfo.InvariantCulture,
            $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R /Info 5 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        Write(output, Ascii(table.ToString()));
        return output.ToArray();
    }

    private static byte[] Ascii(string text) => Encoding.Latin1.GetBytes(text);

    private static void Write(Stream output, byte[] bytes) => output.Write(bytes);

    private static string TitleText(string title) =>
        new([.. title.Select(c => c is '(' or ')' or '\\' ? ' ' : c is >= ' ' and <= '~' ? c : '?')]);
}
