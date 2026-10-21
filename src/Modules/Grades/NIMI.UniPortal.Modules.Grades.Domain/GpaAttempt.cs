namespace NIMI.UniPortal.Modules.Grades.Domain;

/// <summary>One attempt at a subject; the cumulative GPA keeps only the latest attempt (by term start) per subject.</summary>
public sealed record GpaAttempt(Guid SubjectId, DateOnly TermStartsOn, int CreditHours, decimal GradePoints);
