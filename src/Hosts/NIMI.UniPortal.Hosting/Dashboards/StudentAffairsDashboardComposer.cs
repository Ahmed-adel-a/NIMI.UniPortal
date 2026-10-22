using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.Modules.Attendance.Contracts;
using NIMI.UniPortal.Modules.Grades.Contracts;
using NIMI.UniPortal.Modules.Payments.Contracts;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;

namespace NIMI.UniPortal.Hosting.Dashboards;

/// <summary>
/// P10.6 SADASH — composes the Student Affairs summary from module contracts only; counting happens in each module.
/// A faculty/department filter means "these students" (their placement), resolved once from Users and handed to the
/// modules that keep student ids. Each source is read on its own: a failure is logged and reported in <c>unavailable</c>.
/// </summary>
internal sealed class StudentAffairsDashboardComposer(
    IUsersApi users,
    ISchedulingApi scheduling,
    IAcademicLifecycle lifecycle,
    IAttendanceApi attendance,
    IGradesApi grades,
    IPaymentsApi payments,
    TimeProvider clock,
    ILogger<StudentAffairsDashboardComposer> logger)
{
    public async Task<StudentAffairsSummaryDto> ComposeAsync(
        Guid? termId, Guid? facultyId, Guid? departmentId, CancellationToken cancellationToken)
    {
        var values = DashboardMetrics.Catalog.ToDictionary(m => m.Key, _ => (decimal?)null);
        var unavailable = new List<string>();

        var term = termId ?? (await scheduling.GetCurrentTermAsync(cancellationToken))?.Id;
        IReadOnlyCollection<Guid>? scope = facultyId is null && departmentId is null
            ? null
            : await users.GetActiveStudentIdsAsync(facultyId, departmentId, cancellationToken);

        await Read(unavailable, [DashboardMetrics.ActiveStudents], async () =>
            values[DashboardMetrics.ActiveStudents] = await users.CountActiveStudentsAsync(facultyId, departmentId, cancellationToken));
        await Read(unavailable, [DashboardMetrics.GraduationBlocked], async () =>
            values[DashboardMetrics.GraduationBlocked] = await lifecycle.CountGraduationBlockedAsync(facultyId, departmentId, cancellationToken));
        await Read(unavailable, [DashboardMetrics.FeesCollected, DashboardMetrics.FeesOutstanding], async () =>
        {
            var fees = await payments.GetCollectionAsync(term, facultyId, departmentId, cancellationToken);
            values[DashboardMetrics.FeesCollected] = fees.Collected;
            values[DashboardMetrics.FeesOutstanding] = fees.Outstanding;
        });

        if (term is { } id)
        {
            await Read(unavailable, [DashboardMetrics.Registrations], async () =>
                values[DashboardMetrics.Registrations] = await lifecycle.CountConfirmedRegistrationsAsync(id, scope, cancellationToken));
            await Read(unavailable, [DashboardMetrics.AverageAttendance, DashboardMetrics.BarredStudents], async () =>
            {
                var summary = await attendance.GetTermSummaryAsync(id, scope, cancellationToken);
                values[DashboardMetrics.AverageAttendance] = summary.AverageRate;
                values[DashboardMetrics.BarredStudents] = summary.BarredStudents;
            });
            await Read(unavailable, [DashboardMetrics.GradesAwaitingConfirmation, DashboardMetrics.GradesAwaitingPublication], async () =>
            {
                var backlog = await grades.GetBacklogAsync(id, scope, cancellationToken);
                values[DashboardMetrics.GradesAwaitingConfirmation] = backlog.AwaitingConfirmation;
                values[DashboardMetrics.GradesAwaitingPublication] = backlog.AwaitingPublication;
            });
        }

        return new StudentAffairsSummaryDto(term, facultyId, departmentId, values, unavailable, clock.GetUtcNow());
    }

    private async Task Read(List<string> unavailable, string[] keys, Func<Task> read)
    {
        try
        {
            await read();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Dashboard metrics {Metrics} failed; returning them as unavailable.", string.Join(", ", keys));
            unavailable.AddRange(keys);
        }
    }
}
