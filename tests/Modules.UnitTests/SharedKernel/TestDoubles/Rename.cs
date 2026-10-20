using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.UnitTests.SharedKernel.TestDoubles;

/// <summary>A command for the pipeline tests. Name "boom" makes the handler fail.</summary>
public sealed record Rename(string Name) : ICommand<Unit>;
