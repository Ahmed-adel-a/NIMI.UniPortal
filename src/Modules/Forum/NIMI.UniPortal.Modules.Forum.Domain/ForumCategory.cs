namespace NIMI.UniPortal.Modules.Forum.Domain;

/// <summary>A forum section: university-wide (<see cref="FacultyId"/> null) or one faculty's. Managed by Student Affairs.</summary>
public sealed class ForumCategory
{
    public const int MaxNameLength = 100;

    private ForumCategory()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Users faculty id (cross-schema id, no FK); null = everyone.</summary>
    public Guid? FacultyId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string NameAr { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static ForumCategory Create(string name, string nameAr, Guid? facultyId, int sortOrder, DateTimeOffset now)
    {
        var category = new ForumCategory { Id = Guid.CreateVersion7(), IsActive = true, CreatedAt = now };
        category.Apply(name, nameAr, facultyId, sortOrder);
        return category;
    }

    public void Update(string name, string nameAr, Guid? facultyId, int sortOrder, bool isActive, DateTimeOffset now)
    {
        Apply(name, nameAr, facultyId, sortOrder);
        IsActive = isActive;
        UpdatedAt = now;
    }

    private void Apply(string name, string nameAr, Guid? facultyId, int sortOrder)
    {
        Name = ForumText.Required(name, MaxNameLength, nameof(name));
        NameAr = ForumText.Required(nameAr, MaxNameLength, nameof(nameAr));
        FacultyId = facultyId;
        SortOrder = sortOrder;
    }
}
