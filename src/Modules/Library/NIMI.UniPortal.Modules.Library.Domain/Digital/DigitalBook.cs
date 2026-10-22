namespace NIMI.UniPortal.Modules.Library.Domain.Digital;

/// <summary>
/// Decision DIGILIB: a verified e-book Student Affairs adds to the digital library. It is for the whole university
/// (no faculty, no department), one faculty, or one department, and may be linked to a catalog subject. Removing it
/// hides it (the file is kept); downloads are counted.
/// </summary>
public sealed class DigitalBook
{
    public const int MaxDescriptionLength = 2000;

    private DigitalBook()
    {
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Authors { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    /// <summary>Users faculty id (cross-schema, no FK); null with a null department = the whole university.</summary>
    public Guid? FacultyId { get; private set; }

    /// <summary>Users department id; when set, <see cref="FacultyId"/> is that department's faculty.</summary>
    public Guid? DepartmentId { get; private set; }

    /// <summary>Scheduling subject id, for books that belong to a course.</summary>
    public Guid? SubjectId { get; private set; }

    public DigitalBookFormat Format { get; private set; }

    public string FileKey { get; private set; } = string.Empty;

    public string FileName { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    public int DownloadCount { get; private set; }

    public Guid AddedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public DateTimeOffset? RemovedAt { get; private set; }

    public static DigitalBook Add(
        string title, string authors, string? description, Guid? facultyId, Guid? departmentId, Guid? subjectId,
        DigitalBookFormat format, string fileKey, string fileName, long sizeBytes, Guid addedBy, DateTimeOffset now)
    {
        var book = new DigitalBook
        {
            Id = Guid.CreateVersion7(),
            Format = format,
            FileKey = fileKey,
            FileName = Path.GetFileName(fileName),
            SizeBytes = sizeBytes,
            AddedBy = addedBy,
            CreatedAt = now,
        };
        book.Apply(title, authors, description, facultyId, departmentId, subjectId);
        return book;
    }

    public void Edit(string title, string authors, string? description, Guid? facultyId, Guid? departmentId, Guid? subjectId, DateTimeOffset now)
    {
        Apply(title, authors, description, facultyId, departmentId, subjectId);
        UpdatedAt = now;
    }

    public void CountDownload() => DownloadCount++;

    public void Remove(DateTimeOffset now) => RemovedAt ??= now;

    /// <summary>Whether a member of the given faculty and department may see it (Student Affairs sees everything).</summary>
    public bool IsFor(Guid? facultyId, Guid? departmentId) =>
        (FacultyId is null && DepartmentId is null) ||
        (DepartmentId is null && FacultyId == facultyId) ||
        (DepartmentId is not null && DepartmentId == departmentId);

    private void Apply(string title, string authors, string? description, Guid? facultyId, Guid? departmentId, Guid? subjectId)
    {
        Title = LibraryText.Required(title, Book.MaxTitleLength, nameof(title));
        Authors = LibraryText.Required(authors, Book.MaxAuthorsLength, nameof(authors));
        Description = LibraryText.Optional(description, MaxDescriptionLength, nameof(description));
        FacultyId = facultyId;
        DepartmentId = departmentId;
        SubjectId = subjectId;
    }
}
