namespace NIMI.UniPortal.ArchitectureTests;

/// <summary>One .csproj under src/: its module/layer (for module projects) and what it references.</summary>
internal sealed record ProjectNode(
    string Name,
    string Path,
    string? Module,
    string? Layer,
    IReadOnlyList<string> ReferencePaths,
    IReadOnlyList<string> Packages)
{
    public List<ProjectNode> References { get; } = [];

    public override string ToString() => Name;
}
