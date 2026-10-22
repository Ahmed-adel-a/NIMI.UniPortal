// ACCEPTANCE TEST — written by the consultant for phase P04A, revised for decision LIFECYCLEDEPT. Do not modify; report problems instead.
namespace NIMI.UniPortal.ArchitectureTests;

/// <summary>
/// Decision LIFECYCLEDEPT (supersedes LIFECYCLE): every deployment installs every lifecycle variant through the shared
/// hosting project, where the lifecycle router lives; each department picks its academic system as data. Hosts and core
/// modules never reference a variant themselves.
/// </summary>
public sealed class HostCompositionTests
{
    private const string LifecyclePrefix = "Lifecycle.";
    private const string SharedHosting = "NIMI.UniPortal.Hosting";

    [Fact]
    public void The_shared_hosting_project_installs_every_lifecycle_variant()
    {
        var variants = ProjectGraph.ModuleProjects
            .Where(p => p.Module!.StartsWith(LifecyclePrefix, StringComparison.Ordinal) && p.Layer == Layers.Api)
            .Select(p => p.Name)
            .ToList();
        var hosting = ProjectGraph.Projects.Single(p => p.Name == SharedHosting);

        Assert.Equal(2, variants.Count);
        var missing = variants.Where(v => hosting.References.All(r => r.Name != v)).Select(v => $"{SharedHosting} lacks {v}");
        Rule.NoViolations(missing, "Every deployment installs both academic systems (credit hours and year groups)");
    }

    [Fact]
    public void Only_the_shared_hosting_project_references_a_lifecycle_variant()
    {
        var violations = ProjectGraph.Projects
            .Where(p => p.Name != SharedHosting)
            .Where(p => p.Module?.StartsWith(LifecyclePrefix, StringComparison.Ordinal) != true)
            .SelectMany(p => p.References
                .Where(r => r.Module?.StartsWith(LifecyclePrefix, StringComparison.Ordinal) == true)
                .Select(r => $"{p.Name} → {r.Name}"));

        Rule.NoViolations(violations, "Hosts and core modules reach rosters through IAcademicLifecycle (Abstractions), never a variant");
    }
}
