namespace NIMI.UniPortal.Modules.Grades.Application.Publishing;

/// <summary>How many confirmed grades became published (0 when none were waiting).</summary>
public sealed record PublishResultDto(int Published);
