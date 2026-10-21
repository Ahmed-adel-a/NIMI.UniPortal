// ACCEPTANCE TEST — written by the consultant for phase P10. Do not modify; report problems instead.
using System.Text;
using NIMI.UniPortal.SharedKernel.Text;

namespace NIMI.UniPortal.Modules.UnitTests.SharedKernel;

/// <summary>P10.4 — RFC 4180 CSV for exports: quoted only when needed, CRLF rows, UTF-8 with a BOM so Excel shows Arabic.</summary>
public sealed class CsvTests
{
    [Fact]
    public void Plain_fields_are_written_as_is()
    {
        var csv = new Csv();
        csv.Row("CS301", "Algorithms", "3");

        Assert.Equal("CS301,Algorithms,3\r\n", csv.ToString());
    }

    [Fact]
    public void Commas_quotes_and_line_breaks_are_quoted_and_quotes_doubled()
    {
        var csv = new Csv();
        csv.Row("a,b", "say \"hi\"", "two\nlines", null, "");

        Assert.Equal("\"a,b\",\"say \"\"hi\"\"\",\"two\nlines\",,\r\n", csv.ToString());
    }

    [Fact]
    public void Formula_like_text_is_neutralised_so_spreadsheets_do_not_run_it()
    {
        var csv = new Csv();
        csv.Row("=SUM(A1)", "+1", "-2", "@cmd");

        Assert.Equal("'=SUM(A1),'+1,'-2,'@cmd\r\n", csv.ToString());
    }

    [Fact]
    public void Bytes_are_utf8_with_a_bom_and_keep_arabic()
    {
        var csv = new Csv();
        csv.Row("الخوارزميات");

        var bytes = csv.ToUtf8WithBom();

        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);
        Assert.Equal("الخوارزميات\r\n", Encoding.UTF8.GetString(bytes[3..]));
    }
}
