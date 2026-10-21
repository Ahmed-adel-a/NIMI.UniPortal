using NIMI.UniPortal.Modules.Scheduling.Domain.Terms;

namespace NIMI.UniPortal.Modules.Scheduling.Domain.Catalog;

/// <summary>
/// Who teaches what in a term (P3.3, decision MULTIPROF). <see cref="SubjectGroupId"/> null means the whole
/// subject; removing an assignment deactivates it so past timetables stay readable.
/// </summary>
public sealed class TeachingAssignment
{
    private TeachingAssignment()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Users account id — indexed, never a cross-schema FK.</summary>
    public Guid StaffId { get; private set; }

    public Guid SubjectId { get; private set; }

    public Subject Subject { get; private set; } = null!;

    public Guid TermId { get; private set; }

    public Term Term { get; private set; } = null!;

    public Guid? SubjectGroupId { get; private set; }

    public SubjectGroup? SubjectGroup { get; private set; }

    public TeachingRole Role { get; private set; }

    public Guid AssignedBy { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static TeachingAssignment Create(
        Guid staffId,
        Subject subject,
        Term term,
        SubjectGroup? group,
        TeachingRole role,
        Guid assignedBy,
        DateTimeOffset now)
    {
        if (group is not null && (group.SubjectId != subject.Id || group.TermId != term.Id))
            throw new ArgumentException("The group must belong to the same subject and term.", "subjectGroupId");

        if (!Enum.IsDefined(role))
            throw new ArgumentException("Unknown teaching role.", nameof(role));

        return new TeachingAssignment
        {
            Id = Guid.CreateVersion7(),
            StaffId = staffId,
            SubjectId = subject.Id,
            TermId = term.Id,
            SubjectGroupId = group?.Id,
            Role = role,
            AssignedBy = assignedBy,
            IsActive = true,
            CreatedAt = now,
        };
    }

    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAt = now;
    }
}
