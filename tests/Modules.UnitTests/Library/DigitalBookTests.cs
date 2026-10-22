// ACCEPTANCE TEST — written by the consultant for the digital library (decision DIGILIB). Do not modify; report problems instead.
using NIMI.UniPortal.Modules.Library.Domain.Digital;

namespace NIMI.UniPortal.Modules.UnitTests.Library;

/// <summary>Who a digital book is for, and which files are accepted.</summary>
public sealed class DigitalBookTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Eng = Guid.CreateVersion7();
    private static readonly Guid Cs = Guid.CreateVersion7();
    private static readonly Guid Ee = Guid.CreateVersion7();

    [Fact]
    public void A_university_book_is_for_everyone_a_faculty_book_for_its_faculty_a_department_book_for_its_department()
    {
        var university = Book(null, null);
        var faculty = Book(Eng, null);
        var department = Book(Eng, Cs);

        Assert.True(university.IsFor(null, null));
        Assert.True(faculty.IsFor(Eng, Ee));
        Assert.False(faculty.IsFor(Guid.CreateVersion7(), null));
        Assert.True(department.IsFor(Eng, Cs));
        Assert.False(department.IsFor(Eng, Ee));
    }

    [Theory]
    [InlineData("book.pdf", DigitalBookFormat.Pdf)]
    [InlineData("BOOK.EPUB", DigitalBookFormat.Epub)]
    public void Pdf_and_epub_are_accepted(string fileName, DigitalBookFormat format)
    {
        Assert.Equal(format, DigitalBookFiles.Classify(fileName, 1024));
    }

    [Theory]
    [InlineData("book.docx", 1024)]
    [InlineData("book.pdf", 0)]
    [InlineData("book.pdf", DigitalBookFiles.MaxBytes + 1)]
    public void Other_files_empty_files_and_files_over_100_mb_are_rejected(string fileName, long size)
    {
        Assert.Throws<ArgumentException>(() => DigitalBookFiles.Classify(fileName, size));
    }

    [Fact]
    public void Downloads_are_counted_and_removal_is_kept()
    {
        var book = Book(null, null);

        book.CountDownload();
        book.CountDownload();
        book.Remove(Now);
        book.Remove(Now.AddDays(1));

        Assert.Equal(2, book.DownloadCount);
        Assert.Equal(Now, book.RemovedAt);
    }

    private static DigitalBook Book(Guid? facultyId, Guid? departmentId) =>
        DigitalBook.Add("Title", "Author", null, facultyId, departmentId, null, DigitalBookFormat.Pdf, "key", "book.pdf", 10,
            Guid.CreateVersion7(), Now);
}
