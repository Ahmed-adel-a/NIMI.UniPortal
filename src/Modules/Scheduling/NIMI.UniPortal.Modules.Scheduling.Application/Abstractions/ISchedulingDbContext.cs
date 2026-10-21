using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Scheduling.Domain.Catalog;
using NIMI.UniPortal.Modules.Scheduling.Domain.Facilities;
using NIMI.UniPortal.Modules.Scheduling.Domain.Terms;
using NIMI.UniPortal.Modules.Scheduling.Domain.Timetable;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;

/// <summary>The Scheduling module's data seam — the only thing handlers know about persistence.</summary>
public interface ISchedulingDbContext
{
    DbSet<Term> Terms { get; }

    DbSet<Subject> Subjects { get; }

    DbSet<SubjectGroup> SubjectGroups { get; }

    DbSet<TeachingAssignment> TeachingAssignments { get; }

    DbSet<Facility> Facilities { get; }

    DbSet<ScheduleSlot> ScheduleSlots { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
