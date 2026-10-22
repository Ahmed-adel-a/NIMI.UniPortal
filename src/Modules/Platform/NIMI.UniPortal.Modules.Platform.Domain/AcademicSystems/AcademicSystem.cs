namespace NIMI.UniPortal.Modules.Platform.Domain.AcademicSystems;

/// <summary>Decision LIFECYCLEDEPT: how a department runs its studies.</summary>
public enum AcademicSystem
{
    /// <summary>ساعات معتمدة: students register their own subjects within credit limits.</summary>
    CreditHours,

    /// <summary>نظام الفرق: students study their year's plan and are promoted at the end of the year.</summary>
    YearGroups,
}
