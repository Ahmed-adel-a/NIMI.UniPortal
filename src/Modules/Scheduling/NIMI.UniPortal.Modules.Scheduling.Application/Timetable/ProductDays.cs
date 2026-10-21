namespace NIMI.UniPortal.Modules.Scheduling.Application.Timetable;

/// <summary>Product day numbers (0 = Saturday … 6 = Friday) as words, for notification texts.</summary>
internal static class ProductDays
{
    private static readonly string[] English = ["Saturday", "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday"];
    private static readonly string[] Arabic = ["السبت", "الأحد", "الاثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة"];

    public static string En(int day) => English[day];

    public static string Ar(int day) => Arabic[day];
}
