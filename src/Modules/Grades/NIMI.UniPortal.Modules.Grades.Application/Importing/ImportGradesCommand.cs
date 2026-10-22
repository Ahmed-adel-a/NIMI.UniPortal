using NIMI.UniPortal.Modules.Grades.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Grades.Application.Importing;

/// <summary>
/// Decision BACKENDDONE (grades CSV import): the teacher drafts a whole group from a CSV of student codes and scores,
/// all or nothing. Rows without a score are skipped; any bad row is a 400 on <c>rows[line].studentCode</c> or
/// <c>rows[line].score</c> and nothing is saved. Same rules as a typed draft.
/// </summary>
public sealed record ImportGradesCommand(Guid GroupId, IReadOnlyList<GradeCsvRow> Rows, Viewer Caller) : ICommand<GroupGradesDto>
{
    public const int MaxRows = 2000;
}
