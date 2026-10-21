// ACCEPTANCE TEST — written by the consultant for phase P04A. Do not modify; report problems instead.
namespace NIMI.UniPortal.ArchitectureTests;

/// <summary>Decision LIFECYCLE: each deployment (host) composes exactly one lifecycle variant; nothing else picks one.</summary>
public sealed class HostCompositionTests
{
    private const string LifecyclePrefix = "Lifecycle.";

    [Fact]
    public void Every_host_composes_exactly_one_lifecycle_variant()
    {
        var hosts = ProjectGraph.Projects.Where(p => p.Name.StartsWith("Host.", StringComparison.Ordinal)).ToList();

        Assert.NotEmpty(hosts);
        var violations = hosts
            .Select(host => (host, count: host.References.Count(r =>
                r.Module?.StartsWith(LifecyclePrefix, StringComparison.Ordinal) == true && r.Layer == Layers.Api)))
            .Where(x => x.count != 1)
            .Select(x => $"{x.host.Name} references {x.count} lifecycle Api projects");

        Rule.NoViolations(violations, "A host must reference exactly one NIMI.UniPortal.Modules.Lifecycle.*.Api project");
    }

    [Fact]
    public void Only_hosts_reference_a_lifecycle_variant()
    {
        var violations = ProjectGraph.Projects
            .Where(p => !p.Name.StartsWith("Host.", StringComparison.Ordinal))
            .Where(p => p.Module?.StartsWith(LifecyclePrefix, StringComparison.Ordinal) != true)
            .SelectMany(p => p.References
                .Where(r => r.Module?.StartsWith(LifecyclePrefix, StringComparison.Ordinal) == true)
                .Select(r => $"{p.Name} → {r.Name}"));

        Rule.NoViolations(violations, "Core modules reach rosters through IAcademicLifecycle (Abstractions), never a variant");
    }
}
