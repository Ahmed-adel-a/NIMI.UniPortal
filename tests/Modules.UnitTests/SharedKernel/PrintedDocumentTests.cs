// ACCEPTANCE TEST — written by the consultant for PDF exports (decision PDFDOCS). Do not modify; report problems instead.
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using NIMI.UniPortal.SharedKernel.Documents;

namespace NIMI.UniPortal.Modules.UnitTests.SharedKernel;

/// <summary>The built-in PDF writer: a valid PDF 1.4 file with standard fonts, page breaks and safe text.</summary>
public sealed class PrintedDocumentTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 9, 30, 0, TimeSpan.Zero);

    [Fact]
    public void The_file_is_a_pdf_whose_cross_reference_table_points_at_every_object()
    {
        var pdf = Text(new PrintedDocument("Cairo University", "Payment receipt", Now)
            .Fields(("Receipt", "RCP-2026-000001"), ("Amount", "6,500.00 EGP"))
            .ToPdf());

        Assert.StartsWith("%PDF-1.4", pdf);
        Assert.EndsWith("%%EOF\n", pdf);

        var startXref = int.Parse(Regex.Match(pdf, @"startxref\n(\d+)").Groups[1].Value, CultureInfo.InvariantCulture);
        Assert.StartsWith("xref", pdf[startXref..]);
        var offsets = Regex.Matches(pdf[startXref..], @"(\d{10}) 00000 n").Select(m => int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture)).ToList();
        for (var i = 0; i < offsets.Count; i++)
            Assert.StartsWith($"{i + 1} 0 obj", pdf[offsets[i]..]);
    }

    [Fact]
    public void Every_page_shows_the_university_the_title_and_its_number()
    {
        var pdf = Text(new PrintedDocument("Cairo University", "Student schedule", Now).Paragraph("Week of 2026-10-24").ToPdf());

        Assert.Contains("(Cairo University) Tj", pdf);
        Assert.Contains("(Student schedule) Tj", pdf);
        Assert.Contains("(Page 1 of 1) Tj", pdf);
        Assert.Contains("(Generated 2026-10-22 09:30 UTC) Tj", pdf);
        Assert.Contains("/BaseFont /Helvetica", pdf);
    }

    [Fact]
    public void A_long_table_breaks_onto_new_pages()
    {
        var rows = Enumerable.Range(1, 120).Select(i => (IReadOnlyList<string?>)[$"CS{i:000}", $"Subject {i}", "3"]);
        var pdf = Text(new PrintedDocument("University", "Transcript", Now)
            .Table([new TableColumn("Code", 80), new TableColumn("Subject", 300), new TableColumn("Credits", 60, AlignRight: true)], rows)
            .ToPdf());

        var pages = int.Parse(Regex.Match(pdf, @"/Count (\d+)").Groups[1].Value, CultureInfo.InvariantCulture);
        Assert.True(pages >= 3, $"expected at least 3 pages, got {pages}");
        Assert.Equal(pages, Regex.Matches(pdf, @"\(Code\) Tj").Count);   // the header repeats on every page
        Assert.Contains($"(Page {pages} of {pages}) Tj", pdf);
    }

    [Fact]
    public void Text_is_escaped_and_characters_the_standard_fonts_lack_become_question_marks()
    {
        var pdf = Text(new PrintedDocument("University", "Certificate", Now).Paragraph("Fees (paid) \\ عمر").ToPdf());

        Assert.Contains(@"(Fees \(paid\) \\ ???) Tj", pdf);
    }

    private static string Text(byte[] pdf) => Encoding.Latin1.GetString(pdf);
}
