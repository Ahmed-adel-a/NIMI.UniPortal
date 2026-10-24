using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using NIMI.UniPortal.Modules.Forum.Domain;

namespace NIMI.UniPortal.Modules.UnitTests.DomainRules;

/// <summary>
/// The host answers 400 <c>invalid_value</c> only for an <see cref="ArgumentException"/> whose throwing method lives in a
/// <c>.Domain</c> assembly (<c>ApiExceptionHandler.IsDomainRule</c>). In an optimized build the JIT may inline a small
/// domain guard — even a whole entity factory — into the calling handler, so the exception would point at the handler's
/// assembly and a broken business rule would become 500. Domain assemblies are therefore compiled without optimizations
/// (Directory.Build.props): the runtime never inlines a method of a debuggable assembly.
/// </summary>
public sealed class DomainRuleOriginTests
{
    private const string ForumDomain = "NIMI.UniPortal.Modules.Forum.Domain";

    [Fact]
    public void Every_domain_assembly_is_compiled_without_jit_optimizations()
    {
        var violations = new List<string>();
        foreach (var name in DomainProjectNames())
        {
            Assembly assembly;
            try
            {
                assembly = Assembly.Load(name);
            }
            catch (FileNotFoundException)
            {
                violations.Add($"{name} is not referenced by Modules.UnitTests — add a ProjectReference");
                continue;
            }

            if (assembly.GetCustomAttribute<DebuggableAttribute>()?.IsJITOptimizerDisabled != true)
                violations.Add($"{name} is JIT-optimized — its guards can be inlined into handlers (500 instead of 400)");
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    // Meaningful in Release (as CI runs it): a hot caller is re-jitted with inlining. Before the fix the whole
    // ForumCategory.Create → ForumText.Required chain was inlined here and the exception reported this test assembly.
    [Fact]
    public void A_rule_broken_in_a_hot_caller_is_still_reported_by_the_domain()
    {
        for (var round = 0; round < 10; round++)
        {
            for (var i = 0; i < 100; i++)
                CreateCategory("General");
            Thread.Sleep(100); // lets tiered compilation re-jit the hot caller in the background
        }

        var error = Assert.Throws<ArgumentException>(() => CreateCategory(" "));

        Assert.Equal("name", error.ParamName);
        Assert.Equal(ForumDomain, error.TargetSite?.DeclaringType?.Assembly.GetName().Name);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ForumCategory CreateCategory(string name) =>
        ForumCategory.Create(name, "عام", null, 1, DateTimeOffset.UnixEpoch);

    /// <summary>Every NIMI.UniPortal.Modules.*.Domain project under src/Modules.</summary>
    private static IEnumerable<string> DomainProjectNames()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "NIMI.UniPortal.sln")))
            root = root.Parent;
        Assert.NotNull(root);

        return Directory
            .EnumerateFiles(Path.Combine(root.FullName, "src", "Modules"), "*.Domain.csproj", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension)
            .OfType<string>()
            .Order(StringComparer.Ordinal);
    }
}
