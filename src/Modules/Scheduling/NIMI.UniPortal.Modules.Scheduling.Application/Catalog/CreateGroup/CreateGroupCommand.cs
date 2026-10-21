using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Catalog.CreateGroup;

public sealed record CreateGroupCommand(
    Guid SubjectId,
    Guid TermId,
    int GroupNumber,
    Guid? TargetDepartmentId,
    int Capacity) : ICommand<GroupDto>;
