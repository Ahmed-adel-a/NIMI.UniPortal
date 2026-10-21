using NIMI.UniPortal.Modules.Materials.Application.Abstractions;
using NIMI.UniPortal.Modules.Materials.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Materials.Application.Manage;

internal sealed class DeleteMaterialHandler(IMaterialsDbContext db, CourseAccess access, TimeProvider clock)
    : IRequestHandler<DeleteMaterialCommand, Unit>
{
    public async Task<Unit> Handle(DeleteMaterialCommand command, CancellationToken cancellationToken)
    {
        var material = await MaterialOwnership.LoadManageableAsync(db, access, command.MaterialId, command.Caller, cancellationToken);

        material.Delete(clock.GetUtcNow());
        return Unit.Value;
    }
}
