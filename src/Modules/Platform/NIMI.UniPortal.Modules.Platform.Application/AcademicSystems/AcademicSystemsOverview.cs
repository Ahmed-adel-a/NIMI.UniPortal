using NIMI.UniPortal.Modules.Platform.Domain.AcademicSystems;
using NIMI.UniPortal.Modules.Users.Contracts;

namespace NIMI.UniPortal.Modules.Platform.Application.AcademicSystems;

/// <summary>Builds the overview from the rules and the faculties and departments (names from Users).</summary>
internal sealed class AcademicSystemsOverview(AcademicSystemResolver resolver, IUsersApi users)
{
    /// <param name="rules">The rules to show (e.g. a command's result before it is saved); the stored ones when null.</param>
    public async Task<AcademicSystemsOverviewDto> BuildAsync(
        IReadOnlyCollection<AcademicSystemRule>? rules, CancellationToken cancellationToken)
    {
        rules ??= await resolver.RulesAsync(cancellationToken);
        var fallback = resolver.Fallback;
        var university = AcademicSystemRules.Resolve(rules, null, null, fallback);

        var faculties = (await users.GetFacultiesAsync(cancellationToken)).Select(f =>
        {
            var own = rules.FirstOrDefault(r => r.FacultyId == f.Id && r.DepartmentId is null);
            var effective = AcademicSystemRules.Resolve(rules, f.Id, null, fallback);
            return new FacultyAcademicSystemDto(
                f.Id, f.Code, f.Name, f.NameAr, own is null ? null : AcademicSystemNames.Name(own.System),
                AcademicSystemNames.Name(effective.System));
        }).ToList();

        var departments = (await users.GetDepartmentsAsync(null, cancellationToken)).Select(d =>
        {
            var own = rules.FirstOrDefault(r => r.DepartmentId == d.Id);
            var (system, source) = AcademicSystemRules.Resolve(rules, d.FacultyId, d.Id, fallback);
            return new DepartmentAcademicSystemDto(
                d.Id, d.FacultyId, d.Code, d.Name, d.NameAr, own is null ? null : AcademicSystemNames.Name(own.System),
                AcademicSystemNames.Name(system), AcademicSystemNames.Name(source));
        }).ToList();

        return new AcademicSystemsOverviewDto(
            new AcademicSystemDto(AcademicSystemNames.Name(university.System), AcademicSystemNames.Name(university.Source)),
            faculties, departments);
    }
}
