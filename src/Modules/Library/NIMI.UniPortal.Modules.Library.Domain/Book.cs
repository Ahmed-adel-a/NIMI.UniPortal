using System.Runtime.CompilerServices;

namespace NIMI.UniPortal.Modules.Library.Domain;

/// <summary>A catalog title held by the central library (<see cref="FacultyId"/> null) or a faculty library.</summary>
public sealed class Book
{
    public const int MaxTitleLength = 500;
    public const int MaxAuthorsLength = 500;
    public const int MaxPublisherLength = 255;

    private Book()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>ISBN-13, unique when present.</summary>
    public string? Isbn { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Authors { get; private set; } = string.Empty;

    public string? Publisher { get; private set; }

    public int? PublicationYear { get; private set; }

    /// <summary>Users faculty id (cross-schema id, no FK); null = the central library.</summary>
    public Guid? FacultyId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static Book Create(
        string? isbn, string title, string authors, string? publisher, int? publicationYear, Guid? facultyId, DateTimeOffset now)
    {
        var book = new Book { Id = Guid.CreateVersion7(), CreatedAt = now };
        book.Apply(isbn, title, authors, publisher, publicationYear, facultyId, now);
        return book;
    }

    public void Update(string? isbn, string title, string authors, string? publisher, int? publicationYear, Guid? facultyId, DateTimeOffset now)
    {
        Apply(isbn, title, authors, publisher, publicationYear, facultyId, now);
        UpdatedAt = now;
    }

    private void Apply(string? isbn, string title, string authors, string? publisher, int? publicationYear, Guid? facultyId, DateTimeOffset now)
    {
        Isbn = Domain.Isbn.Normalize(isbn);
        Title = LibraryText.Required(title, MaxTitleLength, nameof(title));
        Authors = LibraryText.Required(authors, MaxAuthorsLength, nameof(authors));
        Publisher = LibraryText.Optional(publisher, MaxPublisherLength, nameof(publisher));
        PublicationYear = CheckYear(publicationYear, now);
        FacultyId = facultyId;
    }

    // Never inlined: the host recognises a domain rule (400) by the throwing method's assembly.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int? CheckYear(int? year, DateTimeOffset now)
    {
        if (year is { } value && (value < 1450 || value > now.Year + 1))
            throw new ArgumentOutOfRangeException("publicationYear", year, "Not a plausible publication year.");
        return year;
    }
}
