using System.Text;

namespace NIMI.UniPortal.SharedKernel.Text;

/// <summary>
/// Builds RFC 4180 CSV for exports (P10.4): a field is quoted only when it holds a comma, quote or line break, quotes
/// are doubled, rows end with CRLF. Text starting with = + - @ gets a leading apostrophe so spreadsheets never run it
/// as a formula. <see cref="ToUtf8WithBom"/> adds the BOM Excel needs to show Arabic.
/// </summary>
public sealed class Csv
{
    public const string ContentType = "text/csv; charset=utf-8";

    private readonly StringBuilder text = new();

    public Csv Row(params string?[] fields)
    {
        for (var i = 0; i < fields.Length; i++)
        {
            if (i > 0)
                text.Append(',');
            text.Append(Escape(fields[i]));
        }

        text.Append("\r\n");
        return this;
    }

    public override string ToString() => text.ToString();

    public byte[] ToUtf8WithBom() => [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(text.ToString())];

    private static string Escape(string? field)
    {
        if (string.IsNullOrEmpty(field))
            return string.Empty;

        if (field[0] is '=' or '+' or '-' or '@')
            field = "'" + field;

        return field.IndexOfAny([',', '"', '\r', '\n']) < 0
            ? field
            : "\"" + field.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }
}
