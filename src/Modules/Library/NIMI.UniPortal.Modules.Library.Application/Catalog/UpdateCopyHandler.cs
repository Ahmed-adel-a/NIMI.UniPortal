using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Library.Application.Abstractions;
using NIMI.UniPortal.Modules.Library.Application.Common;
using NIMI.UniPortal.Modules.Library.Domain;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Library.Application.Catalog;

internal sealed class UpdateCopyHandler(ILibraryDbContext db) : IRequestHandler<UpdateCopyCommand, CopyDto>
{
    public async Task<CopyDto> Handle(UpdateCopyCommand command, CancellationToken cancellationToken)
    {
        var status = command.Status?.Trim().ToLowerInvariant() switch
        {
            "available" => CopyStatus.Available,
            "lost" => CopyStatus.Lost,
            "withdrawn" => CopyStatus.Withdrawn,
            _ => throw ValidationErrors.For("status", "Status must be available, lost or withdrawn."),
        };

        var copy = await db.Copies.SingleOrDefaultAsync(c => c.Id == command.CopyId, cancellationToken)
            ?? throw new NotFoundException("Copy not found.");

        LibraryErrors.AsConflict(() => copy.SetCondition(status));
        copy.MoveTo(command.Shelf);
        return CopyDto.From(copy);
    }
}
