using NIMI.UniPortal.Modules.Assignments.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Assignments.Application.Manage;

/// <summary>P8.1: teaching staff delete an assignment nobody has submitted to yet (else 409).</summary>
public sealed record DeleteAssignmentCommand(Guid AssignmentId, Viewer Caller) : ICommand<Unit>;
