using NIMI.UniPortal.Modules.Assignments.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Assignments.Application.Browse;

internal sealed class GetAssignmentHandler(AssignmentAccess access, AssignmentPresenter presenter)
    : IRequestHandler<GetAssignmentQuery, AssignmentDto>
{
    public async Task<AssignmentDto> Handle(GetAssignmentQuery query, CancellationToken cancellationToken)
    {
        var (assignment, teaches) = await access.LoadVisibleAsync(query.AssignmentId, query.Caller, cancellationToken);
        return await presenter.PresentAsync(assignment, query.Caller, staffView: teaches || query.Caller.IsAdmin, cancellationToken);
    }
}
