using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Users.Application.Accounts.ImportStudents;

/// <summary>
/// US-A01 bulk: create student accounts for a whole cohort. Rows are judged one by one — a bad row never blocks the
/// good ones; existing emails are skipped, so re-running the same file is idempotent. All created rows commit together.
/// </summary>
public sealed record ImportStudentsCommand(IReadOnlyList<StudentImportRow> Rows) : ICommand<ImportStudentsReport>
{
    /// <summary>Each account costs a deliberately slow password hash; larger cohorts go in several files.</summary>
    public const int MaxRows = 2000;
}
