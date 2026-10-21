using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Catalog.DeleteTeachingAssignment;

public sealed record DeleteTeachingAssignmentCommand(Guid Id) : ICommand<Unit>;
