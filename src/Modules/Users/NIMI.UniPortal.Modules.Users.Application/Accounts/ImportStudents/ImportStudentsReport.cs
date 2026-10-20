namespace NIMI.UniPortal.Modules.Users.Application.Accounts.ImportStudents;

/// <summary>The per-row outcome of a bulk import, in file order, plus totals.</summary>
public sealed record ImportStudentsReport(int Created, int Skipped, int Failed, IReadOnlyList<ImportRowResult> Rows);
