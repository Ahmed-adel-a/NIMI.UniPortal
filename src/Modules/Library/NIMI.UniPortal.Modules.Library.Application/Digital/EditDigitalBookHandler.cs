using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Library.Application.Abstractions;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Library.Application.Digital;

internal sealed class EditDigitalBookHandler(ILibraryDbContext db, DigitalScope scope, TimeProvider clock)
    : IRequestHandler<EditDigitalBookCommand, DigitalBookDto>
{
    public async Task<DigitalBookDto> Handle(EditDigitalBookCommand command, CancellationToken cancellationToken)
    {
        var book = await db.DigitalBooks.SingleOrDefaultAsync(b => b.Id == command.Id && b.RemovedAt == null, cancellationToken)
            ?? throw new NotFoundException($"No digital book with id '{command.Id}'.");
        var (facultyId, departmentId) = await scope.AudienceAsync(command.FacultyId, command.DepartmentId, command.SubjectId, cancellationToken);

        book.Edit(command.Title ?? string.Empty, command.Authors ?? string.Empty, command.Description, facultyId, departmentId,
            command.SubjectId, clock.GetUtcNow());
        return DigitalBookDto.From(book);
    }
}
