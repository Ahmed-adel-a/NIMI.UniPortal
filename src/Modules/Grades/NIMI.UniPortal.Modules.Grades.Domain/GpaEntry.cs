namespace NIMI.UniPortal.Modules.Grades.Domain;

/// <summary>One published grade as the term GPA sees it.</summary>
public sealed record GpaEntry(int CreditHours, decimal GradePoints);
