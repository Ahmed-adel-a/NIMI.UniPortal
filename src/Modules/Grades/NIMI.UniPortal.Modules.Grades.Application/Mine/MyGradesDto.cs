namespace NIMI.UniPortal.Modules.Grades.Application.Mine;

/// <summary>P9.3 — the student's results, newest term first, with the cumulative GPA over every published term.</summary>
public sealed record MyGradesDto(decimal? CumulativeGpa, IReadOnlyList<MyTermGradesDto> Terms);
