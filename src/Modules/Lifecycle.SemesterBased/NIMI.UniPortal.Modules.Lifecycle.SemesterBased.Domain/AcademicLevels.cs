using System.Runtime.CompilerServices;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain;

/// <summary>Year levels (الفرق) are numbered 1 … 7: enough for a preparatory year plus a six-year programme.</summary>
public static class AcademicLevels
{
    public const int Max = 7;

    /// <summary>Not inlined: the host maps a domain ArgumentException to 400 only when it is thrown inside .Domain.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Valid(int level, string paramName) =>
        level is >= 1 and <= Max ? level : throw new ArgumentOutOfRangeException(paramName, level, $"Between 1 and {Max}.");
}
