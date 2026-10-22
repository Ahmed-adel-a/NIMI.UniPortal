using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Common;
using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Abstractions;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Results;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Graduation;

/// <summary>
/// P9.4 — rule R6 (MILEDU, GRADREQ): eligible ⇔ the department has a requirement, earned credits reach it, and
/// every graduation-requirement subject (military education included) is passed. Students come from Users, one page
/// at a time; ledger rows and requirements are read for that page only. Decision LIFECYCLEDEPT: only students of
/// credit-hours departments are listed (year-group graduation is decided by promotion).
/// </summary>
internal sealed class GraduationEligibilityHandler(
    ICreditHoursDbContext db, IUsersApi users, ISchedulingApi scheduling, CreditHoursStudents creditHours)
    : IRequestHandler<GraduationEligibilityQuery, PagedResult<GraduationEligibilityDto>>
{
    public const string NotConfigured = "requirement_not_configured";
    public const string Credits = "credits";
    public const string RequiredSubjectPrefix = "required_subject:";

    public async Task<PagedResult<GraduationEligibilityDto>> Handle(GraduationEligibilityQuery query, CancellationToken cancellationToken)
    {
        var page = await StudentsAsync(query, cancellationToken);
        var studentIds = page.Items.Select(s => s.StudentId).ToList();
        var departmentIds = page.Items.Select(s => s.DepartmentId).Distinct().ToList();

        var passed = await db.CreditLedger.AsNoTracking()
            .Where(e => studentIds.Contains(e.StudentId) && e.Passed)
            .Select(e => new PassedSubject(e.StudentId, e.SubjectId, e.CreditHours, e.RecordedAt))
            .ToListAsync(cancellationToken);
        var passedByStudent = passed.ToLookup(p => p.StudentId);

        var requirements = await db.GraduationRequirements.AsNoTracking()
            .Where(r => departmentIds.Contains(r.DepartmentId))
            .ToDictionaryAsync(r => r.DepartmentId, r => r.RequiredCredits, cancellationToken);

        var requiredSubjects = (await scheduling.GetGraduationRequirementSubjectsAsync(cancellationToken))
            .OrderBy(s => s.Code, StringComparer.Ordinal)
            .ToList();

        var items = page.Items.Select(student =>
        {
            var mine = passedByStudent[student.StudentId].ToList();
            var earned = EarnedCredits.Of(mine);
            int? required = requirements.TryGetValue(student.DepartmentId, out var credits) ? credits : null;

            var blockers = new List<string>();
            if (required is null)
                blockers.Add(NotConfigured);
            else if (earned < required)
                blockers.Add(Credits);

            var passedSubjects = mine.Select(p => p.SubjectId).ToHashSet();
            blockers.AddRange(requiredSubjects
                .Where(s => !passedSubjects.Contains(s.Id))
                .Select(s => RequiredSubjectPrefix + s.Code));

            return new GraduationEligibilityDto(
                student.StudentId,
                student.FullName,
                student.StudentCode,
                student.DepartmentId,
                blockers.Count == 0,
                earned,
                required,
                blockers);
        }).ToList();

        return new PagedResult<GraduationEligibilityDto>(items, page.Page, page.PageSize, page.TotalCount);
    }

    /// <summary>
    /// The scope's credit-hours students. When every department in scope runs credit hours (the usual case), Users
    /// pages them directly; otherwise the credit-hours departments' students are gathered and paged here.
    /// </summary>
    private async Task<PagedResult<StudentRef>> StudentsAsync(GraduationEligibilityQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<DepartmentRef> departments = query.DepartmentId is { } departmentId
            ? [.. new[] { await users.GetDepartmentAsync(departmentId, cancellationToken) }.OfType<DepartmentRef>()]
            : await users.GetDepartmentsAsync(query.FacultyId, cancellationToken);
        var credit = new List<DepartmentRef>();
        foreach (var department in departments)
        {
            if (await creditHours.IsCreditHoursDepartmentAsync(department.Id, cancellationToken))
                credit.Add(department);
        }

        if (credit.Count == departments.Count)
            return await users.GetStudentsAsync(query.FacultyId, query.DepartmentId, query.Paging, cancellationToken);

        var all = new List<StudentRef>();
        foreach (var department in credit)
        {
            for (var number = 1; ; number++)
            {
                var chunk = await users.GetStudentsAsync(null, department.Id, new PageRequest(number, PageRequest.MaxPageSize), cancellationToken);
                all.AddRange(chunk.Items);
                if (number >= chunk.TotalPages)
                    break;
            }
        }

        var ordered = all.OrderBy(s => s.StudentCode, StringComparer.Ordinal).ThenBy(s => s.StudentId).ToList();
        return new PagedResult<StudentRef>(
            [.. ordered.Skip(query.Paging.Skip).Take(query.Paging.PageSize)], query.Paging.Page, query.Paging.PageSize, ordered.Count);
    }
}
