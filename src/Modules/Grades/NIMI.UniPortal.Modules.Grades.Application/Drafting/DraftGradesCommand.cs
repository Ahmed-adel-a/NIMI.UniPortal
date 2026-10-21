using NIMI.UniPortal.Modules.Grades.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Grades.Application.Drafting;

/// <summary>P9.1: create or overwrite draft grades for roster students of one group.</summary>
public sealed record DraftGradesCommand(Guid GroupId, IReadOnlyList<GradeEntry>? Entries, Viewer Caller) : ICommand<GroupGradesDto>;
