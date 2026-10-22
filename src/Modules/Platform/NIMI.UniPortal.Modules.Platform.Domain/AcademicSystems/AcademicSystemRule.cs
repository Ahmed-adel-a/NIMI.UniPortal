namespace NIMI.UniPortal.Modules.Platform.Domain.AcademicSystems;

/// <summary>
/// Decision LIFECYCLEDEPT: the academic system set at one level. One row for the whole university (no faculty, no
/// department), one per faculty that overrides it, one per department that overrides its faculty. Clearing an
/// override deletes its row.
/// </summary>
public sealed class AcademicSystemRule
{
    private AcademicSystemRule()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Set for a faculty rule only.</summary>
    public Guid? FacultyId { get; private set; }

    /// <summary>Set for a department rule only.</summary>
    public Guid? DepartmentId { get; private set; }

    public AcademicSystem System { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static AcademicSystemRule ForUniversity(AcademicSystem system, DateTimeOffset now) => Create(null, null, system, now);

    public static AcademicSystemRule ForFaculty(Guid facultyId, AcademicSystem system, DateTimeOffset now) =>
        Create(facultyId, null, system, now);

    public static AcademicSystemRule ForDepartment(Guid departmentId, AcademicSystem system, DateTimeOffset now) =>
        Create(null, departmentId, system, now);

    public void Change(AcademicSystem system, DateTimeOffset now)
    {
        System = system;
        UpdatedAt = now;
    }

    private static AcademicSystemRule Create(Guid? facultyId, Guid? departmentId, AcademicSystem system, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(),
        FacultyId = facultyId,
        DepartmentId = departmentId,
        System = system,
        UpdatedAt = now,
    };
}
