using NIMI.UniPortal.Modules.Grades.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Grades.Application.Drafting;

/// <summary>P9.1: the teacher's typed drafts; the rules live in <see cref="GradeDrafter"/>.</summary>
internal sealed class DraftGradesHandler(GradeDrafter drafter) : IRequestHandler<DraftGradesCommand, GroupGradesDto>
{
    public async Task<GroupGradesDto> Handle(DraftGradesCommand command, CancellationToken cancellationToken)
    {
        var graded = await drafter.TeachingGroupAsync(command.GroupId, command.Caller, cancellationToken);
        return await drafter.DraftAsync(graded, command.Entries!, command.Caller, cancellationToken);
    }
}
