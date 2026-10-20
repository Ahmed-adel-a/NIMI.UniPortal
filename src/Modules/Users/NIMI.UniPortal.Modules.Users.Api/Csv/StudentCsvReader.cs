using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using FluentValidation.Results;
using NIMI.UniPortal.Modules.Users.Application.Accounts.ImportStudents;

namespace NIMI.UniPortal.Modules.Users.Api.Csv;

/// <summary>
/// Reads the cohort CSV (UTF-8, header row required):
/// <c>full_name,email,national_id,department_code,enrollment_year</c> — <c>national_id</c> may be empty.
/// File-level problems (missing columns, unreadable file) are a 400 on <c>file</c>; row problems go to the report.
/// </summary>
internal static class StudentCsvReader
{
    public const long MaxBytes = 5 * 1024 * 1024;

    private static readonly string[] RequiredColumns = ["full_name", "email", "national_id", "department_code", "enrollment_year"];

    public static async Task<IReadOnlyList<StudentImportRow>> ReadAsync(Stream stream, CancellationToken cancellationToken)
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
            throw FileError("The file is empty — expected a header row: " + string.Join(",", RequiredColumns));

        var headers = csv.HeaderRecord.Select(h => h.Trim().ToLowerInvariant()).ToHashSet();
        var missing = RequiredColumns.Where(c => !headers.Contains(c)).ToList();
        if (missing.Count > 0)
            throw FileError("Missing column(s): " + string.Join(", ", missing));

        var rows = new List<StudentImportRow>();
        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (csv.Parser.Record?.All(string.IsNullOrWhiteSpace) == true)
                continue;   // blank lines are not rows

            rows.Add(new StudentImportRow(
                rows.Count + 1,
                csv.GetField("full_name"),
                csv.GetField("email"),
                NullIfEmpty(csv.GetField("national_id")),
                csv.GetField("department_code"),
                csv.GetField("enrollment_year")));

            if (rows.Count > ImportStudentsCommand.MaxRows)
                break;   // the command's validator reports the limit
        }

        return rows;
    }

    /// <summary>A 400 <c>validation_failed</c> on the field <c>file</c> (FluentValidation's exception, not CsvHelper's).</summary>
    public static FluentValidation.ValidationException FileError(string message) => new([new ValidationFailure("file", message)]);

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
