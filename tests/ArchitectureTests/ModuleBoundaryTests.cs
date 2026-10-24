namespace NIMI.UniPortal.ArchitectureTests;

/// <summary>Modules talk to each other only through their Contracts (solution-structure.md §3).</summary>
public sealed class ModuleBoundaryTests
{
    [Fact]
    public void Modules_reference_other_modules_through_Contracts_only()
    {
        var violations = ProjectGraph.ModuleProjects
            .SelectMany(p => p.References
                .Where(r => r.Module is not null && r.Module != p.Module && r.Layer != Layers.Contracts)
                .Select(r => $"{p.Name} → {r.Name}"));

        Rule.NoViolations(violations, "Cross-module references must target .Contracts");
    }

    /// <summary>Guards the rules above from passing vacuously (e.g. if src/ moved or the naming changed).</summary>
    [Fact]
    public void Every_module_has_all_five_layer_projects()
    {
        var modules = ProjectGraph.ModuleProjects.GroupBy(p => p.Module!).ToList();

        Assert.Equal(17, modules.Count);   // 16 + StudentReports (decision ROLES2)
        Assert.All(modules, m => Assert.Equal(
            Layers.All.Order(),
            m.Select(p => p.Layer!).Order()));
    }
}
