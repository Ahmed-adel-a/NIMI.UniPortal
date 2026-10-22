namespace NIMI.UniPortal.Hosting.DemoData;

/// <summary>
/// <c>Seed:DemoData</c> = true fills an empty Development database with a demo campus (decision DEVDOCKER); every demo
/// account signs in with <c>Seed:DemoPassword</c>. Without a password nothing is seeded.
/// </summary>
public sealed record DemoDataOptions(bool Enabled, string? Password)
{
    public static DemoDataOptions From(IConfiguration configuration) => new(
        string.Equals(configuration["Seed:DemoData"], "true", StringComparison.OrdinalIgnoreCase),
        configuration["Seed:DemoPassword"]);
}
