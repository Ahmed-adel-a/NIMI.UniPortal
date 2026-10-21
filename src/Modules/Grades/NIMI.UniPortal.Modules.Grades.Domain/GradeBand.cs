namespace NIMI.UniPortal.Modules.Grades.Domain;

/// <summary>One band of a faculty's grading scale (decision SETTINGS): scores from <see cref="MinScore"/> up earn this letter.</summary>
public sealed record GradeBand(string Grade, decimal MinScore, decimal Points);
