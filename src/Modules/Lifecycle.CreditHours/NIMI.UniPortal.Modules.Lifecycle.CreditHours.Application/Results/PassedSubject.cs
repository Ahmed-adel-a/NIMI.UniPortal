namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Results;

/// <summary>One passed ledger row, as earned-credit and graduation checks read it.</summary>
internal sealed record PassedSubject(Guid StudentId, Guid SubjectId, int CreditHours, DateTimeOffset RecordedAt);
