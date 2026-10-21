namespace NIMI.UniPortal.Modules.Scheduling.Domain.Timetable;

/// <summary>How often a timetable slot occurs (P3.5). <c>Once</c> lasts a single date.</summary>
public enum Recurrence
{
    Weekly,
    Biweekly,
    Once,
}
