using System.Globalization;
using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Platform.Contracts;
using NIMI.UniPortal.Modules.Users.Application.Abstractions;
using NIMI.UniPortal.Modules.Users.Domain.Accounts;
using NIMI.UniPortal.Modules.Users.Domain.Organization;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Users.Application.Accounts.ImportStudents;

internal sealed class ImportStudentsHandler(
    IUsersDbContext db,
    IPasswordHasher hasher,
    IStudentCodeAllocator codes,
    IPlatformSettingsApi platform,
    TimeProvider clock) : IRequestHandler<ImportStudentsCommand, ImportStudentsReport>
{
    public async Task<ImportStudentsReport> Handle(ImportStudentsCommand command, CancellationToken cancellationToken)
    {
        // Everything the rows refer to, loaded once.
        var departments = await (
                from d in db.Departments
                join f in db.Faculties on d.FacultyId equals f.Id
                select new { Department = d, Faculty = f })
            .ToDictionaryAsync(x => x.Department.Code, x => (x.Department, x.Faculty), StringComparer.OrdinalIgnoreCase, cancellationToken);

        var fileEmails = command.Rows.Select(r => (r.Email ?? "").Trim().ToLowerInvariant()).Where(e => e.Length > 0).Distinct().ToList();
        var existingEmails = (await db.Users.Where(u => fileEmails.Contains(u.Email)).Select(u => u.Email).ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);

        // Pass 1 — judge every row (no side effects).
        var results = new ImportRowResult?[command.Rows.Count];
        var accepted = new List<(int Index, StudentImportRow Row, string Email, Department Department, Faculty Faculty, int Year)>();
        var firstRowOfEmail = new Dictionary<string, int>(StringComparer.Ordinal);

        for (var i = 0; i < command.Rows.Count; i++)
        {
            var row = command.Rows[i];
            var verdict = Judge(row, departments, existingEmails, firstRowOfEmail, out var email, out var target, out var year);
            if (verdict is not null)
                results[i] = verdict;
            else
                accepted.Add((i, row, email!, target.Department, target.Faculty, year));
        }

        // Hashing is deliberately slow (PBKDF2) — spread it over the CPU cores.
        var temporaryPasswords = accepted.Select(_ => TemporaryPassword.Generate()).ToArray();
        var hashes = new string[accepted.Count];
        Parallel.For(0, accepted.Count, new ParallelOptions { CancellationToken = cancellationToken },
            i => hashes[i] = hasher.Hash(temporaryPasswords[i]));

        // Pass 2 — create in file order, so codes increase down the file.
        var now = clock.GetUtcNow();
        var formats = new Dictionary<Guid, string>();
        for (var i = 0; i < accepted.Count; i++)
        {
            var (index, row, email, department, faculty, year) = accepted[i];
            if (!formats.TryGetValue(faculty.Id, out var format))
                formats[faculty.Id] = format = await platform.GetStudentCodeFormatAsync(faculty.Id, cancellationToken);

            var sequence = await codes.NextAsync(StudentCodeFormat.CounterKey(format, faculty.Code, department.Code, year), cancellationToken);
            var code = StudentCodeFormat.Render(format, faculty.Code, department.Code, year, sequence);
            var details = new PersonalDetails(row.NationalId, null, null, null);

            db.Users.Add(User.CreateStudent(row.FullName!, email, hashes[i], department, year, code, details, now));
            results[index] = new ImportRowResult(row.Row, ImportRowStatus.Created, email, code, temporaryPasswords[i], null);
        }

        var all = results.Select(r => r!).ToList();
        return new ImportStudentsReport(
            all.Count(r => r.Status == ImportRowStatus.Created),
            all.Count(r => r.Status == ImportRowStatus.Skipped),
            all.Count(r => r.Status == ImportRowStatus.Error),
            all);
    }

    /// <summary>Null when the row can be created; otherwise its skipped/error result.</summary>
    private static ImportRowResult? Judge(
        StudentImportRow row,
        Dictionary<string, (Department Department, Faculty Faculty)> departments,
        HashSet<string> existingEmails,
        Dictionary<string, int> firstRowOfEmail,
        out string? email,
        out (Department Department, Faculty Faculty) target,
        out int year)
    {
        email = null;
        target = default;
        year = 0;

        ImportRowResult Error(string message) => new(row.Row, ImportRowStatus.Error, row.Email?.Trim(), null, null, message);

        if (string.IsNullOrWhiteSpace(row.FullName) || row.FullName.Trim().Length > 255)
            return Error("full_name is required (at most 255 characters).");

        try
        {
            email = EmailAddress.Normalize(row.Email ?? "", "email");
        }
        catch (ArgumentException)
        {
            return Error("email is not a valid email address.");
        }

        if (existingEmails.Contains(email))
            return new ImportRowResult(row.Row, ImportRowStatus.Skipped, email, null, null, "An account with this email already exists.");

        if (firstRowOfEmail.TryGetValue(email, out var firstRow))
            return Error($"Duplicate email — already on row {firstRow} of this file.");
        firstRowOfEmail[email] = row.Row;

        if (string.IsNullOrWhiteSpace(row.DepartmentCode) || !departments.TryGetValue(row.DepartmentCode.Trim(), out target))
            return Error($"Unknown department_code '{row.DepartmentCode?.Trim()}'.");
        if (!target.Department.IsActive)
            return Error($"Department '{target.Department.Code}' is deactivated.");

        if (!int.TryParse(row.EnrollmentYear?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out year) || year is < 1950 or > 2100)
            return Error("enrollment_year must be a year between 1950 and 2100.");

        if (row.NationalId?.Trim().Length > 50)
            return Error("national_id is at most 50 characters.");

        return null;
    }
}
