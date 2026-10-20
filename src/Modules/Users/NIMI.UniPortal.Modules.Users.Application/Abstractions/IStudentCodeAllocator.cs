namespace NIMI.UniPortal.Modules.Users.Application.Abstractions;

/// <summary>
/// Hands out the next number for a code prefix atomically in the database (trap #3: never MAX()+1 in code).
/// Numbers are taken immediately, outside the command's transaction — a failed save leaves a gap, never a duplicate.
/// </summary>
public interface IStudentCodeAllocator
{
    Task<int> NextAsync(string counterKey, CancellationToken cancellationToken);
}
