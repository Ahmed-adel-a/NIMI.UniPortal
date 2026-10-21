namespace NIMI.UniPortal.Modules.Platform.Contracts;

/// <summary>One grading-scale band as other modules see it: a score of at least <see cref="MinScore"/> earns <see cref="Grade"/>.</summary>
public sealed record GradeBandRef(string Grade, decimal MinScore, decimal Points);
