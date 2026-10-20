using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.UnitTests.SharedKernel.TestDoubles;

/// <summary>A query for the pipeline tests (queries must never be saved).</summary>
public sealed record Peek : IQuery<int>;
