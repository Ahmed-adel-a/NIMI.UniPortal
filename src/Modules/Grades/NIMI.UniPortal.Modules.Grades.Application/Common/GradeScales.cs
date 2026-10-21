using NIMI.UniPortal.Modules.Grades.Domain;
using NIMI.UniPortal.Modules.Platform.Contracts;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;

namespace NIMI.UniPortal.Modules.Grades.Application.Common;

/// <summary>Rule R5 — the effective grading scale of the faculty that owns the subject (faculty override → university default).</summary>
internal sealed class GradeScales(IUsersApi users, IPlatformSettingsApi platform)
{
    public async Task<GradeScale> ForSubjectAsync(SubjectRef subject, CancellationToken cancellationToken)
    {
        var department = await users.GetDepartmentAsync(subject.DepartmentId, cancellationToken);
        var settings = await platform.GetEffectiveSettingsAsync(department?.FacultyId, cancellationToken);
        return new GradeScale(settings.GradingScale.Select(b => new GradeBand(b.Grade, b.MinScore, b.Points)));
    }
}
