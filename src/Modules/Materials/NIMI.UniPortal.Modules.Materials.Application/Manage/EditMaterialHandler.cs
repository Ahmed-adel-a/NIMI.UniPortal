using NIMI.UniPortal.Modules.Materials.Application.Abstractions;
using NIMI.UniPortal.Modules.Materials.Application.Common;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Materials.Application.Manage;

internal sealed class EditMaterialHandler(
    IMaterialsDbContext db, CourseAccess access, ISchedulingApi scheduling, TimeProvider clock)
    : IRequestHandler<EditMaterialCommand, MaterialDto>
{
    public async Task<MaterialDto> Handle(EditMaterialCommand command, CancellationToken cancellationToken)
    {
        var material = await MaterialOwnership.LoadManageableAsync(db, access, command.MaterialId, command.Caller, cancellationToken);

        material.Edit(command.Title, command.Description, command.WeekNumber, clock.GetUtcNow());

        var code = (await scheduling.GetSubjectAsync(material.SubjectId, cancellationToken))?.Code ?? string.Empty;
        return MaterialDto.From(material, code);
    }
}
