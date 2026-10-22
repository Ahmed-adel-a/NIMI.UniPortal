using System.Globalization;
using System.Text;

namespace NIMI.UniPortal.SharedKernel.Documents;

/// <summary>
/// One page's drawing commands (a PDF content stream). Coordinates are points from the bottom-left corner of an A4
/// page. Text uses the standard fonts F1 (Helvetica) and F2 (Helvetica-Bold) in WinAnsi encoding: characters
/// outside Latin-1 print as '?'.
/// </summary>
internal sealed class PdfPageContent
{
    private readonly StringBuilder commands = new();

    public void Text(double x, double y, string text, double size, bool bold = false, double gray = 0)
    {
        commands.Append(CultureInfo.InvariantCulture,
            $"BT {Num(gray)} g /{(bold ? "F2" : "F1")} {Num(size)} Tf {Num(x)} {Num(y)} Td ({Escape(text)}) Tj ET\n");
    }

    public void Line(double x1, double y1, double x2, double y2, double width = 0.5, double gray = 0.6)
    {
        commands.Append(CultureInfo.InvariantCulture,
            $"{Num(gray)} G {Num(width)} w {Num(x1)} {Num(y1)} m {Num(x2)} {Num(y2)} l S\n");
    }

    public void FillRectangle(double x, double y, double width, double height, double gray)
    {
        commands.Append(CultureInfo.InvariantCulture,
            $"{Num(gray)} g {Num(x)} {Num(y)} {Num(width)} {Num(height)} re f\n");
    }

    public byte[] ToBytes() => Encoding.Latin1.GetBytes(commands.ToString());

    private static string Num(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string Escape(string text)
    {
        var escaped = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            switch (c)
            {
                case '(' or ')' or '\\':
                    escaped.Append('\\').Append(c);
                    break;
                case < ' ':
                    escaped.Append(' ');
                    break;
                case <= '~':
                case >= ' ' and <= 'ÿ':
                    escaped.Append(c);
                    break;
                default:
                    escaped.Append('?');
                    break;
            }
        }

        return escaped.ToString();
    }
}
