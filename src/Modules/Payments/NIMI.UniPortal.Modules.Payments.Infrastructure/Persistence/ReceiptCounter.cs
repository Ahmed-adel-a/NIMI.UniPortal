namespace NIMI.UniPortal.Modules.Payments.Infrastructure.Persistence;

/// <summary>The per-year receipt sequence row; not an aggregate exposed through the application seam.</summary>
internal sealed class ReceiptCounter
{
    public int Year { get; set; }

    public int LastValue { get; set; }
}
