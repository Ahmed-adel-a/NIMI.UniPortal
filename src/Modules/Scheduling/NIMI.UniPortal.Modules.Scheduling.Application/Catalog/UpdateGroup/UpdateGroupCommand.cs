using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Catalog.UpdateGroup;

public sealed record UpdateGroupCommand(
    Guid Id,
    int Capacity,
    Guid? TargetDepartmentId,
    bool IsActive) : ICommand<GroupDto>;
