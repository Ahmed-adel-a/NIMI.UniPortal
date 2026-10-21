namespace NIMI.UniPortal.Modules.Grades.Contracts;

/// <summary>One published final grade: credits and points are the snapshots on the grade; passed ⇔ points &gt; 0.</summary>
public sealed record PublishedGrade(Guid StudentId, Guid SubjectId, int CreditHours, decimal GradePoints, bool Passed);
