namespace NIMI.UniPortal.Modules.Scheduling.Domain.Catalog;

/// <summary>
/// A catalog subject (P3.2) — not tied to a term. The code is immutable and unique (rule R3); removal is
/// deactivation, never deletion, so timetables and history keep pointing at it.
/// </summary>
public sealed class Subject
{
    private Subject()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Users department id — indexed, never a cross-schema FK (ORG).</summary>
    public Guid DepartmentId { get; private set; }

    /// <summary>Trimmed, upper-cased; 2–20 letters or digits (e.g. <c>CS301</c>).</summary>
    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string NameAr { get; private set; } = null!;

    public string? Description { get; private set; }

    public int CreditHours { get; private set; }

    public SubjectCategory Category { get; private set; }

    /// <summary>MILEDU: the military-education subject that counts towards graduation.</summary>
    public bool IsGraduationRequirement { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static Subject Create(
        Guid departmentId,
        string code,
        string name,
        string nameAr,
        string? description,
        int creditHours,
        SubjectCategory category,
        bool isGraduationRequirement,
        DateTimeOffset now)
    {
        var subject = new Subject
        {
            Id = Guid.CreateVersion7(),
            DepartmentId = departmentId,
            Code = NormalizeCode(code, nameof(code)),
            IsActive = true,
            CreatedAt = now,
        };
        subject.Apply(name, nameAr, description, creditHours, category, isGraduationRequirement);
        return subject;
    }

    /// <summary>Edits everything except the code (rule R3: the code never changes).</summary>
    public void Update(
        string name,
        string nameAr,
        string? description,
        int creditHours,
        SubjectCategory category,
        bool isGraduationRequirement,
        bool isActive,
        DateTimeOffset now)
    {
        Apply(name, nameAr, description, creditHours, category, isGraduationRequirement);
        IsActive = isActive;
        UpdatedAt = now;
    }

    private void Apply(
        string name,
        string nameAr,
        string? description,
        int creditHours,
        SubjectCategory category,
        bool isGraduationRequirement)
    {
        Name = Required(name, 255, nameof(name));
        NameAr = Required(nameAr, 255, nameof(nameAr));
        Description = Optional(description, 2000, nameof(description));

        if (creditHours is < 0 or > 12)
            throw new ArgumentException("Between 0 and 12.", nameof(creditHours));

        if (!Enum.IsDefined(category))
            throw new ArgumentException("Unknown subject category.", nameof(category));

        CreditHours = creditHours;
        Category = category;
        IsGraduationRequirement = isGraduationRequirement;
    }

    private static string NormalizeCode(string code, string name)
    {
        var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
        if (normalized.Length is < 2 or > 20 || !normalized.All(char.IsLetterOrDigit))
            throw new ArgumentException("Must be 2–20 letters or digits.", name);
        return normalized;
    }

    private static string Required(string value, int maxLength, string name)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0)
            throw new ArgumentException("Required.", name);
        if (trimmed.Length > maxLength)
            throw new ArgumentException($"At most {maxLength} characters.", name);
        return trimmed;
    }

    private static string? Optional(string? value, int maxLength, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new ArgumentException($"At most {maxLength} characters.", name);
        return trimmed;
    }
}
