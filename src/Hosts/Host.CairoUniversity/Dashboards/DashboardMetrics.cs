namespace Host.CairoUniversity.Dashboards;

/// <summary>P10.6 SADASH — the metric catalog, in display order. Keys are the summary's value names.</summary>
internal static class DashboardMetrics
{
    public const string ActiveStudents = "activeStudents";
    public const string Registrations = "registrations";
    public const string AverageAttendance = "averageAttendance";
    public const string BarredStudents = "barredStudents";
    public const string FeesCollected = "feesCollected";
    public const string FeesOutstanding = "feesOutstanding";
    public const string GradesAwaitingConfirmation = "gradesAwaitingConfirmation";
    public const string GradesAwaitingPublication = "gradesAwaitingPublication";
    public const string GraduationBlocked = "graduationBlocked";

    public static IReadOnlyList<DashboardMetricDto> Catalog { get; } =
    [
        new(ActiveStudents, "Active students", "الطلاب النشطون", "count", "/api/admin/users"),
        new(Registrations, "Course registrations", "تسجيلات المقررات", "count", null),
        new(AverageAttendance, "Average attendance", "متوسط الحضور", "percent", "/api/attendance/at-risk"),
        new(BarredStudents, "Students barred from a final", "طلاب محرومون من امتحان", "count", "/api/attendance/eligibility"),
        new(FeesCollected, "Fees collected", "الرسوم المحصلة", "currency", "/api/admin/payments/report"),
        new(FeesOutstanding, "Fees outstanding", "الرسوم المستحقة", "currency", "/api/admin/payments/report"),
        new(GradesAwaitingConfirmation, "Grades awaiting confirmation", "درجات بانتظار الاعتماد", "count", null),
        new(GradesAwaitingPublication, "Grades awaiting publication", "درجات بانتظار النشر", "count", null),
        new(GraduationBlocked, "Students not yet eligible to graduate", "طلاب غير مستوفين لشروط التخرج", "count", "/api/admin/graduation/eligibility"),
    ];
}
