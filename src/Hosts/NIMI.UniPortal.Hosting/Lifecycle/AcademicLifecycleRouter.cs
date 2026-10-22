using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.Modules.Platform.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;

namespace NIMI.UniPortal.Hosting.Lifecycle;

/// <summary>
/// Decision LIFECYCLEDEPT: the one <see cref="IAcademicLifecycle"/> every module sees. Group questions combine both
/// variants, so a group may mix credit-hours and year-group students; student questions go to the variant of the
/// student's department; counts add both.
/// </summary>
internal sealed class AcademicLifecycleRouter(IEnumerable<ILifecycleVariant> variants, IUsersApi users, IPlatformSettingsApi platform)
    : IAcademicLifecycle
{
    private readonly IReadOnlyList<ILifecycleVariant> all = [.. variants];

    public async Task<IReadOnlyList<Guid>> GetStudentsInGroupAsync(Guid subjectGroupId, CancellationToken cancellationToken = default)
    {
        var students = new List<Guid>();
        foreach (var variant in all)
            students.AddRange(await variant.GetStudentsInGroupAsync(subjectGroupId, cancellationToken));
        return [.. students.Distinct().Order()];
    }

    public async Task<IReadOnlyList<Guid>> GetGroupsForStudentAsync(Guid studentId, Guid termId, CancellationToken cancellationToken = default)
    {
        var groups = new List<Guid>();
        foreach (var variant in all)
            groups.AddRange(await variant.GetGroupsForStudentAsync(studentId, termId, cancellationToken));
        return [.. groups.Distinct().Order()];
    }

    public async Task<bool> IsStudentInGroupAsync(Guid studentId, Guid subjectGroupId, CancellationToken cancellationToken = default)
    {
        foreach (var variant in all)
        {
            if (await variant.IsStudentInGroupAsync(studentId, subjectGroupId, cancellationToken))
                return true;
        }

        return false;
    }

    public async Task<StudentProgress> GetProgressAsync(Guid studentId, CancellationToken cancellationToken = default) =>
        await (await ForStudentAsync(studentId, cancellationToken)).GetProgressAsync(studentId, cancellationToken);

    public async Task<int> CountConfirmedRegistrationsAsync(
        Guid termId, IReadOnlyCollection<Guid>? studentIds, CancellationToken cancellationToken = default)
    {
        var total = 0;
        foreach (var variant in all)
            total += await variant.CountConfirmedRegistrationsAsync(termId, studentIds, cancellationToken);
        return total;
    }

    public async Task<int> CountGraduationBlockedAsync(Guid? facultyId, Guid? departmentId, CancellationToken cancellationToken = default)
    {
        var total = 0;
        foreach (var variant in all)
            total += await variant.CountGraduationBlockedAsync(facultyId, departmentId, cancellationToken);
        return total;
    }

    public Task<bool> HasStudentRecordsAsync(
        string academicSystem, IReadOnlyCollection<Guid> studentIds, CancellationToken cancellationToken = default) =>
        For(academicSystem).HasStudentRecordsAsync(studentIds, cancellationToken);

    private async Task<ILifecycleVariant> ForStudentAsync(Guid studentId, CancellationToken cancellationToken)
    {
        var placement = await users.GetStudentPlacementAsync(studentId, cancellationToken);
        return For(await platform.GetAcademicSystemAsync(placement?.DepartmentId, cancellationToken));
    }

    private ILifecycleVariant For(string academicSystem) =>
        all.FirstOrDefault(v => v.AcademicSystem == academicSystem)
        ?? throw new InvalidOperationException($"No lifecycle variant is installed for '{academicSystem}'.");
}
