namespace NIMI.UniPortal.Modules.Payments.Domain.Ledger;

/// <summary>Rule R3: receipt numbers are human-readable, unique and sequential per year.</summary>
public static class ReceiptNumber
{
    /// <summary>Formats <c>RCP-2026-000042</c>; the sequence always starts at one.</summary>
    public static string Format(int year, int sequence)
    {
        if (year is < 1 or > 9999)
            throw new ArgumentOutOfRangeException(nameof(year), year, "Must be a four-digit year.");

        if (sequence < 1)
            throw new ArgumentOutOfRangeException(nameof(sequence), sequence, "Must be one or more.");

        return $"RCP-{year:0000}-{sequence:000000}";
    }
}
