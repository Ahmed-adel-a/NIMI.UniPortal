using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Catalog.GetSubject;

public sealed record GetSubjectQuery(Guid Id) : IQuery<SubjectDto>;
