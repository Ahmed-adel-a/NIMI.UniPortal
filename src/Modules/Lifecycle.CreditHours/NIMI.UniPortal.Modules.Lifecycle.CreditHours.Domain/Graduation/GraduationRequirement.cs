namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Graduation;

/// <summary>
/// Decision GRADREQ — the credits a department's students need to graduate, set by Student Affairs. A department
/// without a row blocks graduation (<c>requirement_not_configured</c>).
/// </summary>
public sealed class GraduationRequirement
{
    public const int MaxCredits = 400;

    private GraduationRequirement()
    {
    }

    /// <summary>Users department id (cross-schema id, no FK) — also the primary key.</summary>
    public Guid DepartmentId { get; private set; }

    public int RequiredCredits { get; private set; }

    public Guid UpdatedBy { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static GraduationRequirement Create(Guid departmentId, int requiredCredits, Guid updatedBy, DateTimeOffset now)
    {
        var requirement = new GraduationRequirement { DepartmentId = departmentId };
        requirement.Set(requiredCredits, updatedBy, now);
        return requirement;
    }

    public void Set(int requiredCredits, Guid updatedBy, DateTimeOffset now)
    {
        if (requiredCredits is < 1 or > MaxCredits)
            throw new ArgumentOutOfRangeException(nameof(requiredCredits), requiredCredits, $"Between 1 and {MaxCredits}.");

        RequiredCredits = requiredCredits;
        UpdatedBy = updatedBy;
        UpdatedAt = now;
    }
}
