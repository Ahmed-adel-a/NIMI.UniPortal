using NIMI.UniPortal.Abstractions.Files;
using NIMI.UniPortal.Modules.Materials.Application.Abstractions;
using NIMI.UniPortal.Modules.Materials.Application.Common;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Materials.Application.Browse;

internal sealed class DownloadMaterialHandler(IMaterialsDbContext db, CourseAccess access, IFileStorage storage)
    : IRequestHandler<DownloadMaterialCommand, MaterialFileDto>
{
    public async Task<MaterialFileDto> Handle(DownloadMaterialCommand command, CancellationToken cancellationToken)
    {
        var (material, _) = await MaterialVisibility.LoadVisibleAsync(db, access, command.MaterialId, command.Caller, cancellationToken);

        Stream content;
        try
        {
            content = await storage.OpenReadAsync(material.StorageKey, cancellationToken);
        }
        catch (FileNotFoundException)
        {
            throw new NotFoundException("The material's file is missing.");
        }

        material.RecordDownload();
        return new MaterialFileDto(content, material.FileName, material.ContentType);
    }
}
