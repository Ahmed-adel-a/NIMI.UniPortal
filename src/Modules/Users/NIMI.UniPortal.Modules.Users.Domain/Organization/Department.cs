namespace NIMI.UniPortal.Modules.Users.Domain.Organization;

/// <summary>A department inside a faculty. Its code is unique across the whole university.</summary>
public sealed class Department
{
    private Department()
    {
    }

    public Guid Id { get; private set; }

    public Guid FacultyId { get; private set; }

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string NameAr { get; private set; } = null!;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <param name="faculty">Must be active — new departments can't be opened under a closed faculty.</param>
    public static Department Create(Faculty faculty, string code, string name, string nameAr, string? description, DateTimeOffset now)
    {
        RequireActive(faculty);
        return new Department
        {
            Id = Guid.CreateVersion7(),
            FacultyId = faculty.Id,
            Code = OrgCode.Normalize(code, nameof(code)),
            Name = OrgCode.Name(name, nameof(name)),
            NameAr = OrgCode.Name(nameAr, nameof(nameAr)),
            Description = Clean(description),
            IsActive = true,
            CreatedAt = now,
        };
    }

    /// <param name="faculty">The (possibly new) faculty; moving into an inactive faculty is refused.</param>
    public void Update(Faculty faculty, string code, string name, string nameAr, string? description, bool isActive, DateTimeOffset now)
    {
        if (faculty.Id != FacultyId)
            RequireActive(faculty);

        FacultyId = faculty.Id;
        Code = OrgCode.Normalize(code, nameof(code));
        Name = OrgCode.Name(name, nameof(name));
        NameAr = OrgCode.Name(nameAr, nameof(nameAr));
        Description = Clean(description);
        IsActive = isActive;
        UpdatedAt = now;
    }

    private static void RequireActive(Faculty faculty)
    {
        if (!faculty.IsActive)
            throw new ArgumentException("The faculty is deactivated.", "facultyId");
    }

    private static string? Clean(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
