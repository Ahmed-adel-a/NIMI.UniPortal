namespace NIMI.UniPortal.Modules.Users.Application.Accounts.ImportStudents;

/// <summary>
/// One CSV line as read, unvalidated (<c>full_name,email,national_id,department_code,enrollment_year</c>).
/// <see cref="Row"/> is the 1-based line number in the file, header excluded — it's how the report points back.
/// </summary>
public sealed record StudentImportRow(
    int Row,
    string? FullName,
    string? Email,
    string? NationalId,
    string? DepartmentCode,
    string? EnrollmentYear);
