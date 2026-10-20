using System.Reflection;

namespace NIMI.UniPortal.Modules.UnitTests.SharedKernel.TestDoubles;

/// <summary>Which assembly the test unit of work treats as "its own module".</summary>
public sealed record TestModule(Assembly Assembly);
