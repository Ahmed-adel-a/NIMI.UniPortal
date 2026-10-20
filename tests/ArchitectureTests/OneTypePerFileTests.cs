using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NIMI.UniPortal.ArchitectureTests;

/// <summary>
/// Decision ONETYPE: every class, record, struct, interface, enum and delegate lives in its own file —
/// nested types included — and the file is named after it. Checked on the real C# syntax tree (Roslyn)
/// for every .cs file under src/ and tests/.
/// </summary>
public sealed class OneTypePerFileTests
{
    [Fact]
    public void Every_file_declares_exactly_one_type_named_like_the_file()
    {
        var violations = SourceFiles()
            .SelectMany(Check)
            .ToList();

        Rule.NoViolations(violations, "One type per file, named like the file");
    }

    private static IEnumerable<string> Check(string path)
    {
        var relative = Path.GetRelativePath(ProjectGraph.RepoRoot, path);
        var root = CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path).GetCompilationUnitRoot();

        // Every type declaration at any depth (nested ones count too) + delegates.
        var types = root.DescendantNodes()
            .Where(n => n is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax)
            .Select(n => n switch
            {
                BaseTypeDeclarationSyntax t => t.Identifier.Text,
                DelegateDeclarationSyntax d => d.Identifier.Text,
                _ => throw new InvalidOperationException(),
            })
            .ToList();

        if (types.Count > 1)
            yield return $"{relative} declares {types.Count} types: {string.Join(", ", types)}";
        else if (types.Count == 1 && types[0] != FileTypeName(path))
            yield return $"{relative} declares '{types[0]}' — rename the file to {types[0]}.cs";
    }

    /// <summary>"Foo.cs" → Foo; "Foo.Designer.cs" style partial-file suffixes are not used in this repo.</summary>
    private static string FileTypeName(string path) => Path.GetFileNameWithoutExtension(path);

    private static IEnumerable<string> SourceFiles() =>
        new[] { "src", "tests" }
            .Select(dir => Path.Combine(ProjectGraph.RepoRoot, dir))
            .SelectMany(dir => Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories))
            .Where(path => !IsExempt(path));

    /// <summary>Generated or type-less files only — nothing hand-written is exempt.</summary>
    private static bool IsExempt(string path)
    {
        var parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Contains("bin") || parts.Contains("obj")       // build output
            || parts.Contains("Migrations")                          // EF-generated migrations + snapshot
            || Path.GetFileName(path) == "Program.cs";               // top-level statements, no type
    }
}
