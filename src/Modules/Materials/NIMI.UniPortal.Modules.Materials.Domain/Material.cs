namespace NIMI.UniPortal.Modules.Materials.Domain;

/// <summary>
/// A course file a professor publishes for one subject in one term — for every group, or for one group only (D1).
/// Deleting is soft (R4): the row and the stored file stay, the material just disappears from lists and downloads.
/// </summary>
public sealed class Material
{
    private const int MaxTitleLength = 255;
    private const int MaxDescriptionLength = 2000;
    private const int MaxFileNameLength = 255;

    private Material()
    {
    }

    public Guid Id { get; private set; }

    public Guid SubjectId { get; private set; }

    public Guid TermId { get; private set; }

    /// <summary>Null = every group of the subject.</summary>
    public Guid? SubjectGroupId { get; private set; }

    public Guid UploadedBy { get; private set; }

    public string Title { get; private set; } = null!;

    public string? Description { get; private set; }

    public int? WeekNumber { get; private set; }

    /// <summary>Where the file lives in <c>IFileStorage</c>; never derived from user input.</summary>
    public string StorageKey { get; private set; } = null!;

    /// <summary>The original name, used only for the download's Content-Disposition.</summary>
    public string FileName { get; private set; } = null!;

    public string ContentType { get; private set; } = null!;

    public long SizeBytes { get; private set; }

    public MaterialFileType FileType { get; private set; }

    public int DownloadCount { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static Material Create(
        Guid subjectId,
        Guid termId,
        Guid? subjectGroupId,
        Guid uploadedBy,
        string title,
        string? description,
        int? weekNumber,
        string storageKey,
        string fileName,
        string contentType,
        long sizeBytes,
        DateTimeOffset now)
    {
        var cleanFileName = Path.GetFileName(fileName ?? string.Empty).Trim();
        MaterialFiles.EnsureAllowed(cleanFileName, sizeBytes);
        if (cleanFileName.Length > MaxFileNameLength)
            throw new ArgumentException($"At most {MaxFileNameLength} characters in the file name.", "file");

        return new Material
        {
            Id = Guid.CreateVersion7(),
            SubjectId = subjectId,
            TermId = termId,
            SubjectGroupId = subjectGroupId,
            UploadedBy = uploadedBy,
            Title = RequiredTitle(title),
            Description = OptionalDescription(description),
            WeekNumber = Week(weekNumber),
            StorageKey = string.IsNullOrWhiteSpace(storageKey) ? throw new ArgumentException("Required.", nameof(storageKey)) : storageKey,
            FileName = cleanFileName,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim(),
            SizeBytes = sizeBytes,
            FileType = MaterialFiles.Classify(cleanFileName),
            CreatedAt = now,
        };
    }

    /// <summary>Title, description and week only — the file itself never changes.</summary>
    public void Edit(string title, string? description, int? weekNumber, DateTimeOffset now)
    {
        var newTitle = RequiredTitle(title);
        var newDescription = OptionalDescription(description);
        var newWeek = Week(weekNumber);

        Title = newTitle;
        Description = newDescription;
        WeekNumber = newWeek;
        UpdatedAt = now;
    }

    /// <summary>Soft delete (R4); deleting twice changes nothing.</summary>
    public void Delete(DateTimeOffset now)
    {
        if (IsDeleted)
            return;

        IsDeleted = true;
        UpdatedAt = now;
    }

    public void RecordDownload() => DownloadCount++;

    private static string RequiredTitle(string title)
    {
        var trimmed = title?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
            throw new ArgumentException("A title is required.", "title");
        return trimmed.Length <= MaxTitleLength
            ? trimmed
            : throw new ArgumentException($"At most {MaxTitleLength} characters.", "title");
    }

    private static string? OptionalDescription(string? description)
    {
        var trimmed = description?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;
        return trimmed.Length <= MaxDescriptionLength
            ? trimmed
            : throw new ArgumentException($"At most {MaxDescriptionLength} characters.", "description");
    }

    private static int? Week(int? weekNumber) =>
        weekNumber is null or (>= 1 and <= 30)
            ? weekNumber
            : throw new ArgumentOutOfRangeException("weekNumber", weekNumber, "Weeks are numbered 1–30.");
}
