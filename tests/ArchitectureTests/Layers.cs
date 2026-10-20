namespace NIMI.UniPortal.ArchitectureTests;

/// <summary>The five projects every module has (solution-structure.md §2).</summary>
internal static class Layers
{
    public const string Contracts = "Contracts";
    public const string Domain = "Domain";
    public const string Application = "Application";
    public const string Infrastructure = "Infrastructure";
    public const string Api = "Api";

    public static readonly string[] All = [Contracts, Domain, Application, Infrastructure, Api];
}
