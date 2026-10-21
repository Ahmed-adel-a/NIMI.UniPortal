namespace NIMI.UniPortal.Modules.Grades.Application.Corrections;

/// <summary>One audited change to a published grade.</summary>
public sealed record GradeCorrectionDto(
    Guid Id,
    decimal OldScore,
    decimal NewScore,
    string OldGrade,
    string NewGrade,
    string Reason,
    Guid CorrectedBy,
    DateTimeOffset CorrectedAt);
