namespace NIMI.UniPortal.Modules.Grades.Application.Publishing;

/// <summary>How many drafts became confirmed (0 when the group had none left).</summary>
public sealed record ConfirmResultDto(int Confirmed);
