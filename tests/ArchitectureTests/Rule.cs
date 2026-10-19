namespace NIMI.UniPortal.ArchitectureTests;

internal static class Rule
{
    /// <summary>Fails with every violation listed in full (Assert.Empty truncates long collections).</summary>
    public static void NoViolations(IEnumerable<string> violations, string rule)
    {
        var list = violations.ToList();
        Assert.True(list.Count == 0,
            $"{rule} — {list.Count} violation(s):{Environment.NewLine}  " + string.Join(Environment.NewLine + "  ", list));
    }
}
