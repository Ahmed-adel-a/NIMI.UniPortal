using System.Text.RegularExpressions;

namespace NIMI.UniPortal.Modules.Assignments.Domain;

/// <summary>
/// Work a subject's teaching staff set for its students (all groups or one group). Once students have submitted, only
/// the text may change and the deadline may only move later; an assignment with submissions is never deleted.
/// </summary>
public sealed partial class Assignment
{
    private const int MaxTitleLength = 255;
    private const int MaxInstructionsLength = 10000;
    private const decimal MaxGradeLimit = 1000m;

    private Assignment()
    {
    }

    public Guid Id { get; private set; }

    public Guid SubjectId { get; private set; }

    public Guid TermId { get; private set; }

    /// <summary>Null = every group of the subject.</summary>
    public Guid? SubjectGroupId { get; private set; }

    public Guid CreatedBy { get; private set; }

    public string Title { get; private set; } = null!;

    public string? Instructions { get; private set; }

    public DateTimeOffset DueAt { get; private set; }

    public decimal MaxGrade { get; private set; }

    /// <summary>Comma list, lower-case, no dots — e.g. <c>pdf,docx,zip</c>.</summary>
    public string AllowedExtensions { get; private set; } = null!;

    public IReadOnlyList<string> AllowedExtensionList => AllowedExtensions.Split(',');

    public int MaxFileMb { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static Assignment Create(
        Guid subjectId,
        Guid termId,
        Guid? subjectGroupId,
        Guid createdBy,
        string title,
        string? instructions,
        DateTimeOffset dueAt,
        decimal maxGrade,
        string allowedExtensions,
        int maxFileMb,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            SubjectId = subjectId,
            TermId = termId,
            SubjectGroupId = subjectGroupId,
            CreatedBy = createdBy,
            Title = RequiredTitle(title),
            Instructions = OptionalInstructions(instructions),
            DueAt = FutureDue(dueAt, now),
            MaxGrade = Grade(maxGrade),
            AllowedExtensions = Extensions(allowedExtensions),
            MaxFileMb = FileMb(maxFileMb),
            CreatedAt = now,
        };

    public void Update(
        string title,
        string? instructions,
        DateTimeOffset dueAt,
        decimal maxGrade,
        string allowedExtensions,
        int maxFileMb,
        bool hasSubmissions,
        DateTimeOffset now)
    {
        var newTitle = RequiredTitle(title);
        var newInstructions = OptionalInstructions(instructions);
        var newDue = FutureDue(dueAt, now);
        var newGrade = Grade(maxGrade);
        var newExtensions = Extensions(allowedExtensions);
        var newFileMb = FileMb(maxFileMb);

        if (hasSubmissions)
        {
            // Students already submitted against these terms: only the text may change and the deadline may only move later.
            if (newDue < DueAt)
                throw new ArgumentException("After the first submission the deadline can only be extended.", "dueAt");
            if (newGrade != MaxGrade)
                throw new ArgumentException("The maximum grade can't change after the first submission.", "maxGrade");
            if (newExtensions != AllowedExtensions)
                throw new ArgumentException("The allowed file types can't change after the first submission.", "allowedExtensions");
            if (newFileMb != MaxFileMb)
                throw new ArgumentException("The file size limit can't change after the first submission.", "maxFileMb");
        }

        Title = newTitle;
        Instructions = newInstructions;
        DueAt = newDue;
        MaxGrade = newGrade;
        AllowedExtensions = newExtensions;
        MaxFileMb = newFileMb;
        UpdatedAt = now;
    }

    public void Delete(bool hasSubmissions, DateTimeOffset now)
    {
        if (hasSubmissions)
            throw new InvalidOperationException("An assignment with submissions can't be deleted.");
        if (IsDeleted)
            return;

        IsDeleted = true;
        UpdatedAt = now;
    }

    public bool IsOpenAt(DateTimeOffset now) => now < DueAt;

    public bool Allows(string fileName)
    {
        var extension = Path.GetExtension(fileName ?? string.Empty).TrimStart('.').ToLowerInvariant();
        return extension.Length > 0 && AllowedExtensionList.Contains(extension);
    }

    private static string RequiredTitle(string title)
    {
        var trimmed = title?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
            throw new ArgumentException("A title is required.", "title");
        return trimmed.Length <= MaxTitleLength
            ? trimmed
            : throw new ArgumentException($"At most {MaxTitleLength} characters.", "title");
    }

    private static string? OptionalInstructions(string? instructions)
    {
        var trimmed = instructions?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;
        return trimmed.Length <= MaxInstructionsLength
            ? trimmed
            : throw new ArgumentException($"At most {MaxInstructionsLength} characters.", "instructions");
    }

    private static DateTimeOffset FutureDue(DateTimeOffset dueAt, DateTimeOffset now) =>
        dueAt > now ? dueAt : throw new ArgumentException("The deadline must be in the future.", "dueAt");

    private static decimal Grade(decimal maxGrade) =>
        maxGrade > 0m && maxGrade <= MaxGradeLimit && decimal.Round(maxGrade, 2) == maxGrade
            ? maxGrade
            : throw new ArgumentOutOfRangeException("maxGrade", maxGrade, "Between 0.01 and 1000, at most 2 decimals.");

    private static string Extensions(string allowedExtensions)
    {
        var parts = (allowedExtensions ?? string.Empty)
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(e => e.TrimStart('.').ToLowerInvariant())
            .Distinct()
            .ToList();

        if (parts.Count == 0 || parts.Any(e => !ExtensionPattern().IsMatch(e)))
            throw new ArgumentException("A comma list of file extensions, e.g. pdf,docx,zip.", "allowedExtensions");

        return string.Join(',', parts);
    }

    private static int FileMb(int maxFileMb) =>
        maxFileMb is >= 1 and <= 50
            ? maxFileMb
            : throw new ArgumentOutOfRangeException("maxFileMb", maxFileMb, "Between 1 and 50 MB.");

    [GeneratedRegex("^[a-z0-9]{1,10}$")]
    private static partial Regex ExtensionPattern();
}
