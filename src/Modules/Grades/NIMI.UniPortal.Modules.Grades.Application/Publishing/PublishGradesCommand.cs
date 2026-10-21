using NIMI.UniPortal.Modules.Grades.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Grades.Application.Publishing;

/// <summary>P9.2: publish the term's confirmed grades, optionally for one subject or one group.</summary>
public sealed record PublishGradesCommand(Guid? TermId, Guid? SubjectId, Guid? GroupId, Viewer Caller) : ICommand<PublishResultDto>;
