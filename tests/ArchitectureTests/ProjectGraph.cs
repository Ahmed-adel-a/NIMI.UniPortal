using System.Xml.Linq;

namespace NIMI.UniPortal.ArchitectureTests;

/// <summary>
/// The compile-time dependency graph, read straight from the .csproj files under src/.
/// Rules run against declared ProjectReferences rather than compiled assemblies, because
/// the compiler drops references nobody uses yet — an illegal reference would go unnoticed
/// until the first type actually crossed the line.
/// </summary>
internal static class ProjectGraph
{
    private const string ModulePrefix = "NIMI.UniPortal.Modules.";

    public static readonly IReadOnlyList<ProjectNode> Projects = Load();

    public static IEnumerable<ProjectNode> ModuleProjects => Projects.Where(p => p.Module is not null);

    private static List<ProjectNode> Load()
    {
        var src = Path.Combine(FindRepoRoot(), "src");
        var nodes = Directory
            .EnumerateFiles(src, "*.csproj", SearchOption.AllDirectories)
            .Select(Parse)
            .ToList();

        var byPath = nodes.ToDictionary(n => n.Path, StringComparer.OrdinalIgnoreCase);
        foreach (var node in nodes)
        {
            foreach (var target in node.ReferencePaths)
            {
                if (!byPath.TryGetValue(target, out var referenced))
                    throw new InvalidOperationException($"{node.Name} references a project outside src/: {target}");
                node.References.Add(referenced);
            }
        }

        return nodes;
    }

    private static ProjectNode Parse(string csprojPath)
    {
        var name = Path.GetFileNameWithoutExtension(csprojPath);
        var dir = Path.GetDirectoryName(csprojPath)!;
        var xml = XDocument.Load(csprojPath);

        // .csproj files spell paths with '\' (MSBuild convention). On Linux that's not a separator,
        // so normalize before resolving — otherwise CI sees every reference as unresolvable.
        var referencePaths = xml.Descendants("ProjectReference")
            .Select(e => e.Attribute("Include")!.Value.Replace('\\', Path.DirectorySeparatorChar))
            .Select(include => Path.GetFullPath(Path.Combine(dir, include)))
            .ToList();
        var packages = xml.Descendants("PackageReference")
            .Select(e => e.Attribute("Include")!.Value)
            .ToList();

        string? module = null, layer = null;
        if (name.StartsWith(ModulePrefix, StringComparison.Ordinal))
        {
            // NIMI.UniPortal.Modules.<Module>.<Layer> — the module name may itself contain a dot (Lifecycle.CreditHours)
            var rest = name[ModulePrefix.Length..];
            var lastDot = rest.LastIndexOf('.');
            module = rest[..lastDot];
            layer = rest[(lastDot + 1)..];
        }

        return new ProjectNode(name, Path.GetFullPath(csprojPath), module, layer, referencePaths, packages);
    }

    private static string FindRepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "NIMI.UniPortal.sln")))
                return dir.FullName;
        }

        throw new InvalidOperationException("Could not find NIMI.UniPortal.sln above " + AppContext.BaseDirectory);
    }
}

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

internal static class Layers
{
    public const string Contracts = "Contracts";
    public const string Domain = "Domain";
    public const string Application = "Application";
    public const string Infrastructure = "Infrastructure";
    public const string Api = "Api";

    public static readonly string[] All = [Contracts, Domain, Application, Infrastructure, Api];
}
