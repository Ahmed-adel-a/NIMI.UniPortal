using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Policies;

/// <summary>Student Affairs sets the university-wide promotion policy (no faculty) or one faculty's override.</summary>
public sealed record SavePromotionPolicyCommand(
    Guid? FacultyId,
    int MaxCarriedSubjects,
    IReadOnlyList<int>? GateLevels,
    int MaxYearsPerLevel,
    bool RequireFeesPaid) : ICommand<PromotionPolicyDto>;
