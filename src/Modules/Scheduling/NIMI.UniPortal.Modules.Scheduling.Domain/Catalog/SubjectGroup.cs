using NIMI.UniPortal.Modules.Scheduling.Domain.Terms;

namespace NIMI.UniPortal.Modules.Scheduling.Domain.Catalog;

/// <summary>
/// A group (شعبة) a subject offers in one term (P3.3). Only planned/active terms and active subjects may open
/// groups (rule R4); removal is deactivation.
/// </summary>
public sealed class SubjectGroup
{
    private SubjectGroup()
    {
    }

    public Guid Id { get; private set; }

    public Guid SubjectId { get; private set; }

    public Subject Subject { get; private set; } = null!;

    public Guid TermId { get; private set; }

    public Term Term { get; private set; } = null!;

    public int GroupNumber { get; private set; }

    /// <summary>Users department the group is reserved for; null = open to all departments.</summary>
    public Guid? TargetDepartmentId { get; private set; }

    public int Capacity { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static SubjectGroup Create(
        Subject subject,
        Term term,
        int groupNumber,
        Guid? targetDepartmentId,
        int capacity,
        DateTimeOffset now)
    {
        if (term.Status == TermStatus.Closed)
            throw new ArgumentException("Groups cannot be opened in a closed term.", "termId");

        if (!subject.IsActive)
            throw new ArgumentException("Groups need an active subject.", "subjectId");

        var group = new SubjectGroup
        {
            Id = Guid.CreateVersion7(),
            SubjectId = subject.Id,
            TermId = term.Id,
            GroupNumber = groupNumber,
            TargetDepartmentId = targetDepartmentId,
            IsActive = true,
            CreatedAt = now,
        };
        group.Apply(capacity);
        return group;
    }

    public void Update(int capacity, Guid? targetDepartmentId, bool isActive, DateTimeOffset now)
    {
        Apply(capacity);
        TargetDepartmentId = targetDepartmentId;
        IsActive = isActive;
        UpdatedAt = now;
    }

    private void Apply(int capacity)
    {
        if (GroupNumber < 1)
            throw new ArgumentException("At least 1.", "groupNumber");

        if (capacity is < 1 or > 1000)
            throw new ArgumentException("Between 1 and 1000.", nameof(capacity));

        Capacity = capacity;
    }
}
