using System.Globalization;

namespace NIMI.UniPortal.SharedKernel.Documents;

/// <summary>
/// Printable English documents (decision PDFDOCS): receipts, transcripts, certificates and schedules. Every page
/// carries the university's name and the document title; the last line notes when it was generated and the page
/// number. Content flows top to bottom (paragraphs, label/value fields, headings and tables) and breaks onto new
/// pages, repeating a table's header.
/// </summary>
public sealed class PrintedDocument
{
    public const string ContentType = "application/pdf";

    private const double Margin = 50;
    private const double BodySize = 10;
    private const double LineHeight = 14;
    private const double Bottom = Margin + 30;

    private readonly string university;
    private readonly string title;
    private readonly DateTimeOffset generatedAt;
    private readonly List<PdfPageContent> pages = [];
    private PdfPageContent page = null!;
    private double y;

    public PrintedDocument(string university, string title, DateTimeOffset generatedAt)
    {
        this.university = university;
        this.title = title;
        this.generatedAt = generatedAt;
        NewPage();
    }

    private static double ContentWidth => PdfFile.PageWidth - (2 * Margin);

    /// <summary>Wrapped body text.</summary>
    public PrintedDocument Paragraph(string text, bool bold = false)
    {
        foreach (var line in Wrap(text, ContentWidth, bold, BodySize))
        {
            EnsureRoom(LineHeight);
            page.Text(Margin, y, line, BodySize, bold);
            y -= LineHeight;
        }

        y -= LineHeight / 2;
        return this;
    }

    /// <summary>Label/value pairs, labels in bold in a fixed column.</summary>
    public PrintedDocument Fields(params (string Label, string? Value)[] fields)
    {
        const double labelWidth = 150;
        foreach (var (label, value) in fields)
        {
            var lines = Wrap(value ?? "-", ContentWidth - labelWidth, false, BodySize);
            EnsureRoom(LineHeight * lines.Count);
            page.Text(Margin, y, label, BodySize, bold: true);
            foreach (var line in lines)
            {
                page.Text(Margin + labelWidth, y, line, BodySize);
                y -= LineHeight;
            }
        }

        y -= LineHeight / 2;
        return this;
    }

    public PrintedDocument Heading(string text)
    {
        EnsureRoom(LineHeight * 3);
        y -= 4;
        page.Text(Margin, y, text, 12, bold: true);
        y -= 6;
        page.Line(Margin, y, PdfFile.PageWidth - Margin, y);
        y -= LineHeight;
        return this;
    }

    /// <summary>A table; cells that don't fit their column are cut with an ellipsis.</summary>
    public PrintedDocument Table(IReadOnlyList<TableColumn> columns, IEnumerable<IReadOnlyList<string?>> rows)
    {
        TableHeader(columns);
        foreach (var row in rows)
        {
            if (y - LineHeight < Bottom)
            {
                NewPage();
                TableHeader(columns);
            }

            var x = Margin;
            for (var i = 0; i < columns.Count; i++)
            {
                var cell = Fit(i < row.Count ? row[i] ?? string.Empty : string.Empty, columns[i].Width - 6, false, 9);
                var cellX = columns[i].AlignRight ? x + columns[i].Width - 3 - PdfFontMetrics.Measure(cell, false, 9) : x + 3;
                page.Text(cellX, y, cell, 9);
                x += columns[i].Width;
            }

            y -= 4;
            page.Line(Margin, y, Margin + columns.Sum(c => c.Width), y, 0.3, 0.85);
            y -= LineHeight - 4;
        }

        y -= LineHeight / 2;
        return this;
    }

    public PrintedDocument Space(double points)
    {
        y -= points;
        return this;
    }

    public byte[] ToPdf()
    {
        var generated = generatedAt.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture);
        for (var i = 0; i < pages.Count; i++)
        {
            pages[i].Line(Margin, Margin + 14, PdfFile.PageWidth - Margin, Margin + 14);
            pages[i].Text(Margin, Margin, $"Generated {generated}", 8, gray: 0.4);
            var number = $"Page {i + 1} of {pages.Count}";
            pages[i].Text(PdfFile.PageWidth - Margin - PdfFontMetrics.Measure(number, false, 8), Margin, number, 8, gray: 0.4);
        }

        return PdfFile.Write(pages, title);
    }

    private void TableHeader(IReadOnlyList<TableColumn> columns)
    {
        EnsureRoom(LineHeight * 3);
        var width = columns.Sum(c => c.Width);
        page.FillRectangle(Margin, y - 5, width, LineHeight + 2, 0.92);
        var x = Margin;
        foreach (var column in columns)
        {
            var header = Fit(column.Header, column.Width - 6, true, 9);
            var headerX = column.AlignRight ? x + column.Width - 3 - PdfFontMetrics.Measure(header, true, 9) : x + 3;
            page.Text(headerX, y, header, 9, bold: true);
            x += column.Width;
        }

        y -= LineHeight + 2;
    }

    private void NewPage()
    {
        page = new PdfPageContent();
        pages.Add(page);
        y = PdfFile.PageHeight - Margin;
        page.Text(Margin, y, university, 14, bold: true);
        y -= 20;
        page.Text(Margin, y, title, 11, gray: 0.3);
        y -= 10;
        page.Line(Margin, y, PdfFile.PageWidth - Margin, y, 1, 0.2);
        y -= 24;
    }

    private void EnsureRoom(double height)
    {
        if (y - height < Bottom)
            NewPage();
    }

    private static List<string> Wrap(string text, double width, bool bold, double size)
    {
        var lines = new List<string>();
        foreach (var paragraph in text.Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n'))
        {
            var line = string.Empty;
            foreach (var word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var candidate = line.Length == 0 ? word : $"{line} {word}";
                if (line.Length > 0 && PdfFontMetrics.Measure(candidate, bold, size) > width)
                {
                    lines.Add(line);
                    line = Fit(word, width, bold, size);
                }
                else
                {
                    line = Fit(candidate, width, bold, size);
                }
            }

            lines.Add(line);
        }

        return lines;
    }

    private static string Fit(string text, double width, bool bold, double size)
    {
        if (PdfFontMetrics.Measure(text, bold, size) <= width)
            return text;
        var cut = text;
        while (cut.Length > 1 && PdfFontMetrics.Measure(cut + "...", bold, size) > width)
            cut = cut[..^1];
        return cut + "...";
    }
}
