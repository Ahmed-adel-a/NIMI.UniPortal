using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using FluentValidation.Results;
using NIMI.UniPortal.Modules.Grades.Application.Importing;

namespace NIMI.UniPortal.Modules.Grades.Api.Csv;

/// <summary>
/// Reads a grades CSV (UTF-8, header row required): <c>student_code,score</c>; other columns (the template's
/// <c>full_name</c>) are ignored. Comma or semicolon separated. File-level problems are a 400 on <c>file</c>.
/// </summary>
internal static class GradeCsvReader
{
    public const long MaxBytes = 2 * 1024 * 1024;

    private static readonly string[] RequiredColumns = ["student_code", "score"];

    public static async Task<IReadOnlyList<GradeCsvRow>> ReadAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            TrimOptions = TrimOptions.Trim,
            PrepareHeaderForMatch = args => args.Header.Trim().ToLowerInvariant(),
            MissingFieldFound = null,
            BadDataFound = null,
            DetectDelimiter = true,
        });

        if (!await csv.ReadAsync() || !csv.ReadHeader() || csv.HeaderRecord is null)
            throw FileError("The file is empty: expected a header row: " + string.Join(",", RequiredColumns));

        var headers = csv.HeaderRecord.Select(h => h.Trim().ToLowerInvariant()).ToHashSet();
        var missing = RequiredColumns.Where(c => !headers.Contains(c)).ToList();
        if (missing.Count > 0)
            throw FileError("Missing column(s): " + string.Join(", ", missing));

        var rows = new List<GradeCsvRow>();
        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (csv.Parser.Record?.All(string.IsNullOrWhiteSpace) == true)
                continue;   // blank lines are not rows

            rows.Add(new GradeCsvRow(rows.Count + 1, csv.GetField("student_code"), csv.GetField("score")));
            if (rows.Count > ImportGradesCommand.MaxRows)
                break;   // the handler reports the limit
        }

        return rows;
    }

    /// <summary>A 400 <c>validation_failed</c> on the field <c>file</c>.</summary>
    public static FluentValidation.ValidationException FileError(string message) => new([new ValidationFailure("file", message)]);
}
