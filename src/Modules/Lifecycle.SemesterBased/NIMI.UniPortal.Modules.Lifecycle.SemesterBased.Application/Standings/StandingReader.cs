using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Standing;
using NIMI.UniPortal.Modules.Users.Contracts;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Standings;

/// <summary>Adds the student's name and code (from Users) to a standing.</summary>
internal static class StandingReader
{
    public static async Task<StandingDto> ToDtoAsync(StudentStanding standing, IUsersApi users, CancellationToken cancellationToken)
    {
        var student = await users.GetUserAsync(standing.StudentId, cancellationToken);
        return new StandingDto(
            standing.StudentId, student?.FullName ?? string.Empty, student?.StudentCode ?? string.Empty, standing.DepartmentId,
            standing.Level, standing.YearsInLevel, standing.Status);
    }
}
