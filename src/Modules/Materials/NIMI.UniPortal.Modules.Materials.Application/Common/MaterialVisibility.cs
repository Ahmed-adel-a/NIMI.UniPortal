using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Materials.Application.Abstractions;
using NIMI.UniPortal.Modules.Materials.Domain;
using NIMI.UniPortal.SharedKernel;

namespace NIMI.UniPortal.Modules.Materials.Application.Common;

/// <summary>
/// One material as seen by one caller (R2, R4): deleted or invisible materials are a 404 — the API never tells a
/// caller that something they may not see exists.
/// </summary>
internal static class MaterialVisibility
{
    public static async Task<(Material Material, bool Teaches)> LoadVisibleAsync(
        IMaterialsDbContext db,
        CourseAccess access,
        Guid materialId,
        Viewer viewer,
        CancellationToken cancellationToken)
    {
        var material = await db.Materials.SingleOrDefaultAsync(m => m.Id == materialId && !m.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Material not found.");

        if (viewer.IsAdmin)
            return (material, false);

        if (viewer.IsProfessor && await access.TeachesSubjectAsync(viewer.UserId, material.SubjectId, material.TermId, cancellationToken))
            return (material, true);

        if (viewer.IsStudent)
        {
            var groups = await access.StudentGroupsAsync(viewer.UserId, material.SubjectId, material.TermId, cancellationToken);
            if (groups.Count > 0 && (material.SubjectGroupId is null || groups.Contains(material.SubjectGroupId.Value)))
                return (material, false);
        }

        throw new NotFoundException("Material not found.");
    }
}
