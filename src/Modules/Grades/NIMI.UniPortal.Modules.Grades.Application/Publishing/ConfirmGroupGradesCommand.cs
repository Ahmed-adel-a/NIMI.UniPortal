using NIMI.UniPortal.Modules.Grades.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Grades.Application.Publishing;

/// <summary>P9.2: Student Affairs confirms a group's drafts once every roster student has a grade (R3).</summary>
public sealed record ConfirmGroupGradesCommand(Guid GroupId, Viewer Caller) : ICommand<ConfirmResultDto>;
