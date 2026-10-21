using NIMI.UniPortal.Modules.Assignments.Application.Browse;
using NIMI.UniPortal.Modules.Assignments.Application.Common;
using NIMI.UniPortal.Modules.Assignments.Contracts;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Assignments.Application;

/// <summary>The Assignments contract, answered by the student's own list query (visibility rules in one place).</summary>
internal sealed class AssignmentsApi(ISender sender) : IAssignmentsApi
{
    public async Task<IReadOnlyList<UpcomingAssignmentRef>> GetUpcomingForStudentAsync(
        Guid studentId, DateTimeOffset dueBefore, CancellationToken cancellationToken = default)
    {
        var viewer = new Viewer(studentId, false, false, true);
        var upcoming = new List<UpcomingAssignmentRef>();
        for (var page = 1; ; page++)
        {
            var open = await sender.Send(
                new ListAssignmentsQuery(null, "open", viewer, new PageRequest(page, PageRequest.MaxPageSize)), cancellationToken);
            upcoming.AddRange(open.Items
                .Where(a => a.MySubmissionStatus is null && a.DueAt <= dueBefore)
                .Select(a => new UpcomingAssignmentRef(a.Id, a.SubjectId, a.SubjectCode, a.Title, a.DueAt)));
            if (page >= open.TotalPages)
                break;
        }

        return [.. upcoming.OrderBy(a => a.DueAt).ThenBy(a => a.Id)];
    }
}
