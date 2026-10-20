using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.UnitTests.SharedKernel.TestDoubles;

/// <summary>A query that returns its own text — the simplest possible request for dispatch tests.</summary>
public sealed record Echo(string Text) : IQuery<string>;
