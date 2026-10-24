namespace NIMI.UniPortal.ArchitectureTests;

/// <summary>
/// The OpenAPI document names every request and response shape after its C# type, so two modules with a
/// <c>ReportDto</c> show one module's shape for the other's routes (and the generated frontend types are wrong).
/// Request bodies (<c>*Request</c>) and returned data (<c>*Dto</c>) are therefore named uniquely across the whole API.
/// One type per file makes the file name the type name.
/// </summary>
public sealed class ApiTypeNameTests
{
    [Fact]
    public void Request_and_response_types_have_unique_names_across_modules()
    {
        var duplicates = Directory.EnumerateFiles(Path.Combine(ProjectGraph.RepoRoot, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(p => p is "bin" or "obj" or "Migrations"))
            .Select(path => (Name: Path.GetFileNameWithoutExtension(path), Path: Path.GetRelativePath(ProjectGraph.RepoRoot, path)))
            .Where(f => f.Name.EndsWith("Dto", StringComparison.Ordinal) || f.Name.EndsWith("Request", StringComparison.Ordinal))
            .GroupBy(f => f.Name)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key}: {string.Join(", ", g.Select(f => f.Path))}");

        Rule.NoViolations(duplicates, "API types (*Dto, *Request) need unique names; prefix them with the module's word");
    }
}
