using System.Globalization;

namespace NIMI.UniPortal.Modules.Assignments.Domain;

/// <summary>The receipt a student keeps as proof of submission (R6): <c>SUB-2026-000001</c>, sequential per year.</summary>
public static class SubmissionReceipt
{
    public static string Format(int year, int sequence)
    {
        if (sequence < 1)
            throw new ArgumentOutOfRangeException(nameof(sequence), sequence, "Sequences start at 1.");
        return string.Create(CultureInfo.InvariantCulture, $"SUB-{year:0000}-{sequence:000000}");
    }
}
