namespace NIMI.UniPortal.Modules.Users.Application.Accounts.ImportStudents;

/// <summary>What happened to one line. Created rows carry the student code and the one-time temporary password.</summary>
public sealed record ImportRowResult(
    int Row,
    ImportRowStatus Status,
    string? Email,
    string? StudentCode,
    string? TemporaryPassword,
    string? Error);
