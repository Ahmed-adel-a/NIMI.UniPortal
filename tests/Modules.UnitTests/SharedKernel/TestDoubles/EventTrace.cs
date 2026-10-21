// ACCEPTANCE TEST — written by the consultant for phase P02. Do not modify; report problems instead.
namespace NIMI.UniPortal.Modules.UnitTests.SharedKernel.TestDoubles;

/// <summary>Records, in order, what happened during a command: "handled", "saved", "first:&lt;name&gt;", "second:&lt;name&gt;".</summary>
public sealed class EventTrace
{
    public List<string> Entries { get; } = [];

    /// <summary>When true, the next unit-of-work save throws (simulates a failed commit).</summary>
    public bool FailSave { get; set; }
}
