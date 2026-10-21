using NIMI.UniPortal.Modules.Grades.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Grades.Application.Mine;

/// <summary>P9.3: the signed-in student's published grades, optionally for one term.</summary>
public sealed record MyGradesQuery(Guid? TermId, Viewer Caller) : IQuery<MyGradesDto>;
