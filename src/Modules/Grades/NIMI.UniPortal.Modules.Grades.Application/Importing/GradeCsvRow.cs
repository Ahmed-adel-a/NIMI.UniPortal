namespace NIMI.UniPortal.Modules.Grades.Application.Importing;

/// <summary>One data line of a grades CSV, as text (line 1 is the first row after the header).</summary>
public sealed record GradeCsvRow(int Line, string? StudentCode, string? Score);
