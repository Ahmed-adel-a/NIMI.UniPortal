namespace NIMI.UniPortal.SharedKernel.Messaging;

/// <summary>For commands with nothing to return.</summary>
public readonly record struct Unit
{
    public static readonly Unit Value = default;
}
