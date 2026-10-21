using NIMI.UniPortal.Modules.Grades.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Grades.Application.Drafting;

/// <summary>P9.1: the group's roster with each student's grade row (teaching staff or Student Affairs).</summary>
public sealed record GetGroupGradesQuery(Guid GroupId, Viewer Caller) : IQuery<GroupGradesDto>;
