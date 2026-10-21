using NIMI.UniPortal.Modules.Assignments.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Assignments.Application.Manage;

/// <summary>P8.1: a teaching professor sets an assignment for a subject (all groups or one).</summary>
public sealed record CreateAssignmentCommand(
    Guid SubjectId,
    Guid? SubjectGroupId,
    string Title,
    string? Instructions,
    DateTimeOffset DueAt,
    decimal MaxGrade,
    string AllowedExtensions,
    int MaxFileMb,
    Viewer Caller) : ICommand<AssignmentDto>;
