using System.Runtime.CompilerServices;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Promotion;

/// <summary>
/// Decision SEMPROMO: the promotion numbers Student Affairs sets. There is one university-wide row (no faculty) and
/// optional per-faculty overrides. Without any row the built-in <see cref="DefaultRules"/> apply.
/// </summary>
public sealed class PromotionPolicy
{
    public const int MaxCarriedLimit = 10;
    public const int MaxYearsLimit = 5;

    private PromotionPolicy()
    {
    }

    /// <summary>3 carried subjects (the owner's default), no gates, 2 years per level (art. 80), fees must be paid.</summary>
    public static PromotionRuleSet DefaultRules { get; } = new(3, [], 2, true);

    public Guid Id { get; private set; }

    /// <summary>Null for the university-wide row.</summary>
    public Guid? FacultyId { get; private set; }

    public int MaxCarriedSubjects { get; private set; }

    public int[] GateLevels { get; private set; } = [];

    public int MaxYearsPerLevel { get; private set; }

    public bool RequireFeesPaid { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public PromotionRuleSet Rules => new(MaxCarriedSubjects, GateLevels, MaxYearsPerLevel, RequireFeesPaid);

    public static PromotionPolicy CreateDefault(
        int maxCarriedSubjects, IReadOnlyCollection<int> gateLevels, int maxYearsPerLevel, bool requireFeesPaid, DateTimeOffset now) =>
        Create(null, maxCarriedSubjects, gateLevels, maxYearsPerLevel, requireFeesPaid, now);

    public static PromotionPolicy CreateForFaculty(
        Guid facultyId, int maxCarriedSubjects, IReadOnlyCollection<int> gateLevels, int maxYearsPerLevel, bool requireFeesPaid,
        DateTimeOffset now) =>
        Create(facultyId, maxCarriedSubjects, gateLevels, maxYearsPerLevel, requireFeesPaid, now);

    public void Update(
        int maxCarriedSubjects, IReadOnlyCollection<int> gateLevels, int maxYearsPerLevel, bool requireFeesPaid, DateTimeOffset now)
    {
        MaxCarriedSubjects = InRange(maxCarriedSubjects, 0, MaxCarriedLimit, nameof(maxCarriedSubjects));
        MaxYearsPerLevel = InRange(maxYearsPerLevel, 1, MaxYearsLimit, nameof(maxYearsPerLevel));
        GateLevels = Gates(gateLevels);
        RequireFeesPaid = requireFeesPaid;
        UpdatedAt = now;
    }

    private static PromotionPolicy Create(
        Guid? facultyId, int maxCarriedSubjects, IReadOnlyCollection<int> gateLevels, int maxYearsPerLevel, bool requireFeesPaid,
        DateTimeOffset now)
    {
        var policy = new PromotionPolicy { Id = Guid.CreateVersion7(), FacultyId = facultyId };
        policy.Update(maxCarriedSubjects, gateLevels, maxYearsPerLevel, requireFeesPaid, now);
        return policy;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int InRange(int value, int min, int max, string paramName) =>
        value >= min && value <= max ? value : throw new ArgumentOutOfRangeException(paramName, value, $"Between {min} and {max}.");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int[] Gates(IReadOnlyCollection<int> gateLevels)
    {
        // A gate at level 1 means nothing: nobody enters level 1 from below.
        if (gateLevels.Any(g => g is < 2 or > AcademicLevels.Max))
            throw new ArgumentOutOfRangeException(nameof(gateLevels), $"Gate levels are between 2 and {AcademicLevels.Max}.");
        return [.. gateLevels.Distinct().Order()];
    }
}
