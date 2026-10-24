namespace NIMI.UniPortal.Modules.Audit.Contracts;

/// <summary>What an activity row is about (P10.2); the entity id is the owning module's id.</summary>
public static class AuditEntities
{
    public const string SystemSettings = "system_settings";
    public const string Faculty = "faculty";
    public const string User = "user";
    public const string SubjectGroup = "subject_group";
    public const string Term = "term";
    public const string FinalGrade = "final_grade";
    public const string Payment = "payment";
    public const string DeviceChangeRequest = "device_change_request";
    public const string ScheduleSlot = "schedule_slot";
    public const string Department = "department";
    public const string PromotionPolicy = "promotion_policy";
    public const string AcademicSystemRule = "academic_system_rule";
    public const string StudentReport = "student_report";
}
