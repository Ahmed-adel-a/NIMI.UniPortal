using NIMI.UniPortal.Abstractions.Files;
using NIMI.UniPortal.Modules.Library.Application.Abstractions;
using NIMI.UniPortal.Modules.Library.Domain;
using NIMI.UniPortal.Modules.Library.Domain.Digital;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Library.Application.Digital;

internal sealed class AddDigitalBookHandler(ILibraryDbContext db, DigitalScope scope, IFileStorage files, TimeProvider clock)
    : IRequestHandler<AddDigitalBookCommand, DigitalBookDto>
{
    public async Task<DigitalBookDto> Handle(AddDigitalBookCommand command, CancellationToken cancellationToken)
    {
        if (command.Content is null)
            throw ValidationErrors.For("file", "Attach the book in the form field 'file'.");

        // Every check before the file is stored, so a rejected upload leaves nothing behind.
        var format = DigitalBookFiles.Classify(command.FileName, command.FileSize);
        LibraryText.Required(command.Title, Book.MaxTitleLength, "title");
        LibraryText.Required(command.Authors, Book.MaxAuthorsLength, "authors");
        var (facultyId, departmentId) = await scope.AudienceAsync(command.FacultyId, command.DepartmentId, command.SubjectId, cancellationToken);

        var stored = await files.SaveAsync(command.Content, command.FileName!, DigitalBookFiles.ContentType(format), cancellationToken);
        var book = DigitalBook.Add(
            command.Title!, command.Authors!, command.Description, facultyId, departmentId, command.SubjectId, format, stored.Key,
            command.FileName!, stored.SizeBytes, command.AdminId, clock.GetUtcNow());
        db.DigitalBooks.Add(book);
        return DigitalBookDto.From(book);
    }
}
