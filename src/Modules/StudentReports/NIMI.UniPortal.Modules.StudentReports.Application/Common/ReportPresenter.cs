using NIMI.UniPortal.Modules.StudentReports.Domain;
using NIMI.UniPortal.Modules.Users.Contracts;

namespace NIMI.UniPortal.Modules.StudentReports.Application.Common;

/// <summary>Turns reports into DTOs, naming the students and heads with one Users call per page.</summary>
internal sealed class ReportPresenter(IUsersApi users)
{
    public async Task<StudentReportDto> PresentAsync(StudentReport report, CancellationToken cancellationToken) =>
        (await PresentAllAsync([report], cancellationToken))[0];

    public async Task<IReadOnlyList<StudentReportDto>> PresentAllAsync(
        IReadOnlyList<StudentReport> reports, CancellationToken cancellationToken)
    {
        var ids = reports.Select(r => r.StudentId)
            .Concat(reports.Where(r => r.AnsweredBy != null).Select(r => r.AnsweredBy!.Value))
            .Distinct()
            .ToList();
        var people = (await users.GetUsersAsync(ids, cancellationToken)).ToDictionary(u => u.Id);

        return reports.Select(r =>
        {
            var student = people.GetValueOrDefault(r.StudentId);
            var head = r.AnsweredBy is { } headId ? people.GetValueOrDefault(headId) : null;
            return new StudentReportDto(
                r.Id, r.StudentId, student?.FullName ?? string.Empty, student?.StudentCode, r.DepartmentId, r.Category,
                r.Title, r.Message, r.Status, r.Reply, r.AnsweredBy, head?.FullName, r.AnsweredAt, r.ClosedAt, r.CreatedAt);
        }).ToList();
    }
}
