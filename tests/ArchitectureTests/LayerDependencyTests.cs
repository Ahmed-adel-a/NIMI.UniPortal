namespace NIMI.UniPortal.ArchitectureTests;

/// <summary>Clean Architecture inside each module (solution-structure.md §2).</summary>
public sealed class LayerDependencyTests
{
    [Fact]
    public void Domain_projects_reference_nothing()
    {
        var violations = ProjectGraph.ModuleProjects
            .Where(p => p.Layer == Layers.Domain)
            .Where(p => p.ReferencePaths.Count > 0 || p.Packages.Count > 0)
            .Select(p => $"{p.Name} → {string.Join(", ", p.References.Select(r => r.Name).Concat(p.Packages))}");

        Rule.NoViolations(violations, "Domain must reference nothing");
    }

    [Fact]
    public void Application_projects_never_reference_Infrastructure_or_Api()
    {
        var violations = ProjectGraph.ModuleProjects
            .Where(p => p.Layer == Layers.Application)
            .SelectMany(p => p.References
                .Where(r => r.Layer is Layers.Infrastructure or Layers.Api)
                .Select(r => $"{p.Name} → {r.Name}"));

        Rule.NoViolations(violations, "Application must not reference Infrastructure or Api");
    }
}
