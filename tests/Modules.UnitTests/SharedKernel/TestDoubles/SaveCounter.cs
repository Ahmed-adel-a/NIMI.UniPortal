namespace NIMI.UniPortal.Modules.UnitTests.SharedKernel.TestDoubles;

/// <summary>Records, in order, when the handler ran and when the unit of work saved.</summary>
public sealed class SaveCounter
{
    public List<string> Trace { get; } = [];

    public int Handled => Trace.Count(t => t == "handled");

    public int Saved => Trace.Count(t => t == "saved");
}
