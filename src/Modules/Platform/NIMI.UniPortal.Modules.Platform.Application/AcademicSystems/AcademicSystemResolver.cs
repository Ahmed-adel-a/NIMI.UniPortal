using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NIMI.UniPortal.Modules.Platform.Application.Abstractions;
using NIMI.UniPortal.Modules.Platform.Domain.AcademicSystems;

namespace NIMI.UniPortal.Modules.Platform.Application.AcademicSystems;

/// <summary>Reads the rules and resolves a department's (or faculty's, or the university's) academic system.</summary>
internal sealed class AcademicSystemResolver(IPlatformDbContext db, IOptions<AcademicSystemDefaults> defaults)
{
    public AcademicSystem Fallback =>
        AcademicSystemNames.TryParse(defaults.Value.Default, out var system) ? system : AcademicSystem.CreditHours;

    public Task<List<AcademicSystemRule>> RulesAsync(CancellationToken cancellationToken) =>
        db.AcademicSystemRules.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<(AcademicSystem System, AcademicSystemSource Source)> ResolveAsync(
        Guid? facultyId, Guid? departmentId, CancellationToken cancellationToken)
    {
        var rules = await db.AcademicSystemRules.AsNoTracking()
            .Where(r => (r.FacultyId == null && r.DepartmentId == null) ||
                        (facultyId != null && r.FacultyId == facultyId) ||
                        (departmentId != null && r.DepartmentId == departmentId))
            .ToListAsync(cancellationToken);
        return AcademicSystemRules.Resolve(rules, facultyId, departmentId, Fallback);
    }
}
