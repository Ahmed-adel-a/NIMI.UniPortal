namespace NIMI.UniPortal.Modules.Grades.Application.Drafting;

/// <summary>One student's score (0–100, two decimals) in a draft.</summary>
public sealed record GradeEntry(Guid StudentId, decimal Score);
