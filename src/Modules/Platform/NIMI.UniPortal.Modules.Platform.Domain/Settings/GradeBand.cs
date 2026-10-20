namespace NIMI.UniPortal.Modules.Platform.Domain.Settings;

/// <summary>One row of a grading scale: a score of at least <see cref="MinScore"/> earns <see cref="Grade"/> and <see cref="Points"/>.</summary>
public sealed record GradeBand(string Grade, decimal MinScore, decimal Points);
