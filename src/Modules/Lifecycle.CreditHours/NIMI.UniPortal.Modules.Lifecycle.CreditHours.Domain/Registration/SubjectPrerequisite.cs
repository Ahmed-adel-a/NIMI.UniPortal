namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;

/// <summary>
/// One "subject requires another subject" edge (P4.2, decision PREREQ). A pair is unique, and a subject can never
/// require itself; the API additionally rejects a cycle of length two. No cross-schema foreign keys.
/// </summary>
public sealed class SubjectPrerequisite
{
    private SubjectPrerequisite()
    {
    }

    public Guid Id { get; private set; }

    public Guid SubjectId { get; private set; }

    public Guid PrerequisiteSubjectId { get; private set; }

    public static SubjectPrerequisite Create(Guid subjectId, Guid prerequisiteSubjectId)
    {
        if (subjectId == prerequisiteSubjectId)
            throw new ArgumentException("A subject cannot require itself.", nameof(prerequisiteSubjectId));

        return new SubjectPrerequisite
        {
            Id = Guid.CreateVersion7(),
            SubjectId = subjectId,
            PrerequisiteSubjectId = prerequisiteSubjectId,
        };
    }
}
