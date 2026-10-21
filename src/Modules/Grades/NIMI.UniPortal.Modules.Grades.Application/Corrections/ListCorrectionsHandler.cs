using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Grades.Application.Abstractions;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Grades.Application.Corrections;

internal sealed class ListCorrectionsHandler(IGradesDbContext db)
    : IRequestHandler<ListCorrectionsQuery, IReadOnlyList<GradeCorrectionDto>>
{
    public async Task<IReadOnlyList<GradeCorrectionDto>> Handle(ListCorrectionsQuery query, CancellationToken cancellationToken)
    {
        if (!await db.FinalGrades.AsNoTracking().AnyAsync(g => g.Id == query.GradeId, cancellationToken))
            throw new NotFoundException("Grade not found.");

        return await db.GradeCorrections.AsNoTracking()
            .Where(c => c.FinalGradeId == query.GradeId)
            .OrderBy(c => c.CorrectedAt)
            .ThenBy(c => c.Id)
            .Select(c => new GradeCorrectionDto(
                c.Id, c.OldScore, c.NewScore, c.OldGrade, c.NewGrade, c.Reason, c.CorrectedBy, c.CorrectedAt))
            .ToListAsync(cancellationToken);
    }
}
