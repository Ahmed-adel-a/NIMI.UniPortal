using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Abstractions;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Promotion;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Policies;

/// <summary>The faculty's override, else the university-wide row, else the built-in defaults.</summary>
internal sealed class EffectivePolicies(ISemesterDbContext db)
{
    public const string FacultySource = "faculty";
    public const string UniversitySource = "university";
    public const string DefaultSource = "default";

    public async Task<PromotionPolicyDto> ForAsync(Guid? facultyId, CancellationToken cancellationToken)
    {
        var rows = await db.PromotionPolicies.AsNoTracking()
            .Where(p => p.FacultyId == null || (facultyId != null && p.FacultyId == facultyId))
            .ToListAsync(cancellationToken);

        var faculty = facultyId is null ? null : rows.SingleOrDefault(p => p.FacultyId == facultyId);
        var university = rows.SingleOrDefault(p => p.FacultyId == null);
        var (rules, source) = faculty is not null
            ? (faculty.Rules, FacultySource)
            : university is not null
                ? (university.Rules, UniversitySource)
                : (PromotionPolicy.DefaultRules, DefaultSource);

        return new PromotionPolicyDto(
            facultyId, source, rules.MaxCarriedSubjects, rules.GateLevels, rules.MaxYearsPerLevel, rules.RequireFeesPaid);
    }

    public async Task<PromotionRuleSet> RulesForAsync(Guid? facultyId, CancellationToken cancellationToken)
    {
        var policy = await ForAsync(facultyId, cancellationToken);
        return new PromotionRuleSet(policy.MaxCarriedSubjects, policy.GateLevels, policy.MaxYearsPerLevel, policy.RequireFeesPaid);
    }
}
