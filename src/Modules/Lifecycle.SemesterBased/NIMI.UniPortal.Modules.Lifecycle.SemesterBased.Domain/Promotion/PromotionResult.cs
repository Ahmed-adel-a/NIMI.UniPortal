namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Promotion;

/// <summary>The year-end outcome for one student (decision SEMPROMO).</summary>
public enum PromotionResult
{
    Promoted,
    PromotedWithCarried,
    Repeat,
    External,
    Graduated,
    Held,
}
