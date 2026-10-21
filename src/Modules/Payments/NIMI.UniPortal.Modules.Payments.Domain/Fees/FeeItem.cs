namespace NIMI.UniPortal.Modules.Payments.Domain.Fees;

/// <summary>
/// A fee Student Affairs defines once (PAYOWN). It is deactivated, never deleted (rule R8), and targets the whole
/// university, one faculty or one department. Amounts are snapshotted into <see cref="StudentFee"/> on first payment.
/// </summary>
public sealed class FeeItem
{
    private FeeItem()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string NameAr { get; private set; } = null!;

    public string? Description { get; private set; }

    public FeeCategory Category { get; private set; }

    public decimal Amount { get; private set; }

    /// <summary>Scheduling term id; null means annual / one-off (cross-schema id, no FK).</summary>
    public Guid? TermId { get; private set; }

    /// <summary>Users faculty id; null with no department means the whole university.</summary>
    public Guid? FacultyId { get; private set; }

    /// <summary>Users department id; a department implies its faculty.</summary>
    public Guid? DepartmentId { get; private set; }

    public DateOnly DueOn { get; private set; }

    public bool IsActive { get; private set; }

    public Guid CreatedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>Creates an active fee item; validates the amount shape rule R1.</summary>
    public static FeeItem Create(
        string name,
        string nameAr,
        string? description,
        FeeCategory category,
        decimal amount,
        Guid? termId,
        Guid? facultyId,
        Guid? departmentId,
        DateOnly dueOn,
        Guid createdBy,
        DateTimeOffset now)
    {
        if (!Enum.IsDefined(category))
            throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown fee category.");

        EnsureAmount(amount);

        return new FeeItem
        {
            Id = Guid.CreateVersion7(),
            Name = Required(name, nameof(name), 255),
            NameAr = Required(nameAr, nameof(nameAr), 255),
            Description = Optional(description),
            Category = category,
            Amount = amount,
            TermId = termId,
            FacultyId = facultyId,
            DepartmentId = departmentId,
            DueOn = dueOn,
            IsActive = true,
            CreatedBy = createdBy,
            CreatedAt = now,
        };
    }

    /// <summary>Edits the mutable details; the target, category and term never change after creation.</summary>
    public void Update(
        string name,
        string nameAr,
        string? description,
        decimal amount,
        DateOnly dueOn,
        bool isActive,
        DateTimeOffset now)
    {
        EnsureAmount(amount);

        Name = Required(name, nameof(name), 255);
        NameAr = Required(nameAr, nameof(nameAr), 255);
        Description = Optional(description);
        Amount = amount;
        DueOn = dueOn;
        IsActive = isActive;
        UpdatedAt = now;
    }

    /// <summary>
    /// §6 applicability: active, not military education (that flow creates the student fee directly), and targeted
    /// at the whole university, the student's faculty, or the student's department.
    /// </summary>
    public bool AppliesTo(Guid studentFacultyId, Guid studentDepartmentId)
    {
        if (!IsActive || Category == FeeCategory.MilitaryEducation)
            return false;

        if (FacultyId is null && DepartmentId is null)
            return true;

        if (DepartmentId is { } department)
            return department == studentDepartmentId;

        return FacultyId == studentFacultyId;
    }

    private static void EnsureAmount(decimal amount)
    {
        if (amount <= 0 || amount != decimal.Round(amount, 2))
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Must be positive with at most 2 decimals.");
    }

    private static string Required(string? value, string field, int maxLength)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw new ArgumentException("Required.", field);

        if (trimmed.Length > maxLength)
            throw new ArgumentException($"Must be at most {maxLength} characters.", field);

        return trimmed;
    }

    private static string? Optional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.Length > 2000)
            throw new ArgumentException("Must be at most 2000 characters.", "description");

        return trimmed;
    }
}
