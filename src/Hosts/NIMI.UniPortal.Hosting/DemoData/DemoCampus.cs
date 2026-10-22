namespace NIMI.UniPortal.Hosting.DemoData;

/// <summary>
/// The demo campus's well-known accounts, and the ids the shared seeder created in this run, so a host's premium
/// seeders (registered after it, same scope) can build on them.
/// <see cref="Seeded"/> is false when nothing was built.
/// </summary>
public sealed class DemoCampus
{
    public const string AdminEmail = "affairs@demo.cu.edu.eg";
    public const string ProfessorEmail = "hassan.mahmoud@demo.cu.edu.eg";
    public const string StudentEmail = "omar.khaled@demo.cu.edu.eg";
    public const string NewStudentEmail = "new.student@demo.cu.edu.eg";

    public bool Seeded { get; internal set; }

    public Guid AdminId { get; internal set; }

    public Guid EngineeringFacultyId { get; internal set; }

    public Guid ProfessorId { get; internal set; }

    public Guid StudentId { get; internal set; }

    public IReadOnlyList<Guid> StudentIds { get; internal set; } = [];

    public Guid TermId { get; internal set; }

    /// <summary>CSE101 Introduction to Programming — what the demo student and professor share.</summary>
    public Guid ProgrammingSubjectId { get; internal set; }
}
