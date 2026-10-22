namespace NIMI.UniPortal.Modules.Audit.Contracts;

/// <summary>The audited actions (P10.2), <c>entity.verb</c> in snake_case.</summary>
public static class AuditActions
{
    public const string SettingsUpdated = "settings.updated";
    public const string FacultySettingsUpdated = "faculty_settings.updated";
    public const string FacultySettingsRemoved = "faculty_settings.removed";
    public const string UserCreated = "user.created";
    public const string UserStatusChanged = "user.status_changed";
    public const string UserPasswordReset = "user.password_reset";
    public const string GradesConfirmed = "grades.confirmed";
    public const string GradesPublished = "grades.published";
    public const string GradeCorrected = "grade.corrected";
    public const string PaymentRecorded = "payment.recorded";
    public const string DeviceChangeDecided = "device_change.decided";
    public const string ScheduleSlotUpdated = "schedule_slot.updated";
    public const string ScheduleSlotCancelled = "schedule_slot.cancelled";
    public const string StudyPlanSaved = "study_plan.saved";
    public const string AcademicSystemChanged = "academic_system.changed";
    public const string PromotionPolicyUpdated = "promotion_policy.updated";
    public const string PromotionPolicyRemoved = "promotion_policy.removed";
    public const string StandingPlaced = "standing.placed";
    public const string PromotionOverridden = "promotion.overridden";
}
