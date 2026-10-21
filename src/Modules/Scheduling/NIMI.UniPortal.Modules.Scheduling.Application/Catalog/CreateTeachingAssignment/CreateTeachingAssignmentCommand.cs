using NIMI.UniPortal.Modules.Scheduling.Domain.Catalog;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Catalog.CreateTeachingAssignment;

public sealed record CreateTeachingAssignmentCommand(
    Guid StaffId,
    Guid SubjectId,
    Guid TermId,
    Guid? SubjectGroupId,
    TeachingRole Role,
    Guid AssignedBy) : ICommand<TeachingAssignmentDto>;
