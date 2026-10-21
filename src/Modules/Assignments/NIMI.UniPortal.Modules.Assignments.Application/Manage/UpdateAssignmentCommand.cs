using NIMI.UniPortal.Modules.Assignments.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Assignments.Application.Manage;

/// <summary>P8.1: teaching staff edit an assignment (limited once students have submitted).</summary>
public sealed record UpdateAssignmentCommand(
    Guid AssignmentId,
    string Title,
    string? Instructions,
    DateTimeOffset DueAt,
    decimal MaxGrade,
    string AllowedExtensions,
    int MaxFileMb,
    Viewer Caller) : ICommand<AssignmentDto>;
