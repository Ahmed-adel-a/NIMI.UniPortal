namespace NIMI.UniPortal.Modules.Users.Domain.Accounts;

/// <summary>Optional personal data captured at account creation (US-A01).</summary>
public sealed record PersonalDetails(string? NationalId, DateOnly? DateOfBirth, Gender? Gender, string? Phone)
{
    public static readonly PersonalDetails None = new(null, null, null, null);
}
