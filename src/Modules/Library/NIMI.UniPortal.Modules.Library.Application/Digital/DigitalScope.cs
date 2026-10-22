using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Library.Application.Digital;

/// <summary>
/// Decision DIGILIB: who a digital book is for. A department implies its faculty; a member sees university-wide
/// books, their faculty's and their department's. Student Affairs sees everything.
/// </summary>
internal sealed class DigitalScope(IUsersApi users, ISchedulingApi scheduling)
{
    /// <summary>Checks the requested audience and subject; returns the stored faculty and department.</summary>
    public async Task<(Guid? FacultyId, Guid? DepartmentId)> AudienceAsync(
        Guid? facultyId, Guid? departmentId, Guid? subjectId, CancellationToken cancellationToken)
    {
        if (subjectId is { } subject && await scheduling.GetSubjectAsync(subject, cancellationToken) is null)
            throw ValidationErrors.For("subjectId", "Unknown subject.");

        if (departmentId is { } department)
        {
            var found = await users.GetDepartmentAsync(department, cancellationToken)
                ?? throw ValidationErrors.For("departmentId", "Unknown department.");
            if (facultyId is not null && facultyId != found.FacultyId)
                throw ValidationErrors.For("departmentId", "The department belongs to another faculty.");
            return (found.FacultyId, found.Id);
        }

        if (facultyId is { } faculty && await users.GetFacultyAsync(faculty, cancellationToken) is null)
            throw ValidationErrors.For("facultyId", "Unknown faculty.");
        return (facultyId, null);
    }

    /// <summary>The reader's faculty and department (both null for accounts without a department).</summary>
    public async Task<(Guid? FacultyId, Guid? DepartmentId)> ReaderAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await users.GetUserAsync(userId, cancellationToken)
            ?? throw new ForbiddenException("Your account was not found.");
        if (user.DepartmentId is not { } departmentId)
            return (null, null);
        var department = await users.GetDepartmentAsync(departmentId, cancellationToken);
        return (department?.FacultyId, departmentId);
    }
}
