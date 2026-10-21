using NIMI.UniPortal.Modules.Materials.Application.Abstractions;
using NIMI.UniPortal.Modules.Materials.Application.Common;
using NIMI.UniPortal.Modules.Materials.Domain;
using NIMI.UniPortal.SharedKernel;

namespace NIMI.UniPortal.Modules.Materials.Application.Manage;

/// <summary>Editing and deleting: invisible → 404 (no existence leak); visible but not the uploader or a teacher → 403.</summary>
internal static class MaterialOwnership
{
    public static async Task<Material> LoadManageableAsync(
        IMaterialsDbContext db, CourseAccess access, Guid materialId, Viewer viewer, CancellationToken cancellationToken)
    {
        var (material, teaches) = await MaterialVisibility.LoadVisibleAsync(db, access, materialId, viewer, cancellationToken);
        if (!teaches && material.UploadedBy != viewer.UserId)
            throw new ForbiddenException("Only the uploader or a teacher of the subject can change this material.");

        return material;
    }
}
