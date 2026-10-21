namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Results;

/// <summary>
/// Credits earned = the credit hours of every distinct subject the student has passed (a subject passed twice
/// counts once, with its most recently recorded credits). Shared by progress and graduation eligibility.
/// </summary>
internal static class EarnedCredits
{
    public static int Of(IEnumerable<PassedSubject> passed) =>
        passed
            .GroupBy(p => p.SubjectId)
            .Sum(g => g.MaxBy(p => p.RecordedAt)!.CreditHours);
}
