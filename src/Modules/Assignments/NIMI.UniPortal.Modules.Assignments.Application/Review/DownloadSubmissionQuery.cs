using NIMI.UniPortal.Modules.Assignments.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Assignments.Application.Review;

/// <summary>P8.4: the submitted file, for the student who handed it in or the subject's teaching staff (anyone else 404).</summary>
public sealed record DownloadSubmissionQuery(Guid SubmissionId, Viewer Caller) : IQuery<SubmissionFileDto>;
