using NIMI.UniPortal.Modules.Assignments.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Assignments.Application.Browse;

/// <summary>P8.2: one assignment the caller may see (404 otherwise).</summary>
public sealed record GetAssignmentQuery(Guid AssignmentId, Viewer Caller) : IQuery<AssignmentDto>;
