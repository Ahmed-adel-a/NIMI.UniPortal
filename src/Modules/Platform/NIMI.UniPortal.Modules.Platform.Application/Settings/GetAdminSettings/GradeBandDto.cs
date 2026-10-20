namespace NIMI.UniPortal.Modules.Platform.Application.Settings.GetAdminSettings;

/// <summary>One grading-scale row over HTTP: a score of at least <see cref="MinScore"/> earns <see cref="Grade"/>.</summary>
public sealed record GradeBandDto(string Grade, decimal MinScore, decimal Points);
