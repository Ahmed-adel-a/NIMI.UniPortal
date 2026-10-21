namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Results;

/// <summary>One ledger row as the GPA maths sees it, with its term's start date for "latest attempt" ordering.</summary>
public sealed record ResultLine(Guid SubjectId, Guid TermId, DateOnly TermStartsOn, int CreditHours, decimal GradePoints, bool Passed);
