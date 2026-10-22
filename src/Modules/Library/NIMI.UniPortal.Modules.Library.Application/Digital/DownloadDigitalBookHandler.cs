using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Abstractions.Files;
using NIMI.UniPortal.Modules.Library.Application.Abstractions;
using NIMI.UniPortal.Modules.Library.Domain.Digital;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Library.Application.Digital;

internal sealed class DownloadDigitalBookHandler(ILibraryDbContext db, DigitalScope scope, IFileStorage files)
    : IRequestHandler<DownloadDigitalBookCommand, DigitalBookFile>
{
    public async Task<DigitalBookFile> Handle(DownloadDigitalBookCommand command, CancellationToken cancellationToken)
    {
        var book = await db.DigitalBooks.SingleOrDefaultAsync(b => b.Id == command.Id && b.RemovedAt == null, cancellationToken)
            ?? throw new NotFoundException($"No digital book with id '{command.Id}'.");
        if (!command.Caller.IsAdmin)
        {
            var (facultyId, departmentId) = await scope.ReaderAsync(command.Caller.UserId, cancellationToken);
            if (!book.IsFor(facultyId, departmentId))
                throw new NotFoundException($"No digital book with id '{command.Id}'.");
        }

        book.CountDownload();
        var content = await files.OpenReadAsync(book.FileKey, cancellationToken);
        return new DigitalBookFile(content, DigitalBookFiles.ContentType(book.Format), book.FileName);
    }
}
