namespace NIMI.UniPortal.Hosting.DemoData;

/// <summary>
/// Decision NATIONALIDLOGIN, Development only: <c>Seed:TestAccount:NationalId</c> and <c>Seed:TestAccount:Password</c>
/// (user secrets or the git-ignored appsettings.Local.json, never the repo) give the owner one login for every role.
/// Without both values nothing is seeded.
/// </summary>
public sealed record TestAccountOptions(string? NationalId, string? Password)
{
    public bool Enabled => !string.IsNullOrWhiteSpace(NationalId) && !string.IsNullOrWhiteSpace(Password);

    public static TestAccountOptions From(IConfiguration configuration) =>
        new(configuration["Seed:TestAccount:NationalId"]?.Trim(), configuration["Seed:TestAccount:Password"]);
}
