namespace NIMI.UniPortal.Modules.Grades.Domain;

/// <summary>The audit row of one change to a published grade (rule R4).</summary>
public sealed class GradeCorrection
{
    public const int MaxReasonLength = 1000;

    private GradeCorrection()
    {
    }

    public Guid Id { get; private set; }

    public Guid FinalGradeId { get; private set; }

    public decimal OldScore { get; private set; }

    public decimal NewScore { get; private set; }

    public string OldGrade { get; private set; } = string.Empty;

    public string NewGrade { get; private set; } = string.Empty;

    public string Reason { get; private set; } = string.Empty;

    public Guid CorrectedBy { get; private set; }

    public DateTimeOffset CorrectedAt { get; private set; }

    internal static GradeCorrection Record(
        Guid finalGradeId,
        decimal oldScore,
        decimal newScore,
        string oldGrade,
        string newGrade,
        string reason,
        Guid correctedBy,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            FinalGradeId = finalGradeId,
            OldScore = oldScore,
            NewScore = newScore,
            OldGrade = oldGrade,
            NewGrade = newGrade,
            Reason = reason,
            CorrectedBy = correctedBy,
            CorrectedAt = now,
        };
}
